using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using XlfParser.Model;
using System.Threading;
using LiteDB;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TranslationTools
{
    public class TranslateXlfLlm
    {
        string[] languages;
        string provider;
        string apiKey;
        string model;
        string appContext;

        public string SystemPrompt { get; set; }
        static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) };

        int TranslateCount = 0;
        int TranslateLocalDb = 0;
        int TranslateLlm = 0;
        int TranslateErrors = 0;

        void UpdateStatus()
        {
            try
            {
                Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
            }
            catch
            {
                Console.Write("\r");
            }
            Console.Write("Processed:{0} Pulled from database:{1} Translated by {2}:{3} Errors:{4}", TranslateCount, TranslateLocalDb, provider, TranslateLlm, TranslateErrors);
        }

        public TranslateXlfLlm()
        {

        }
        public void DoTheWork(string InputFile, string ProductName, string ConfigFile, bool ShowOutput, bool OffLineMode)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigFile);
            languages = ConfigurationManager.AppSettings["Languages"].Split(',');

            provider = ConfigurationManager.AppSettings["LLMProvider"];
            if (string.IsNullOrEmpty(provider))
                provider = "ChatGPT";

            appContext = string.IsNullOrEmpty(SystemPrompt) ? ConfigurationManager.AppSettings["LLMSystemPrompt"] : SystemPrompt;

            if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
            {
                provider = "Claude";
                apiKey = ConfigurationManager.AppSettings["ClaudeKey"];
                model = ConfigurationManager.AppSettings["ClaudeModel"];
                if (string.IsNullOrEmpty(model))
                    model = "claude-sonnet-4-5";
            }
            else if (provider.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) ||
                     provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                provider = "ChatGPT";
                apiKey = ConfigurationManager.AppSettings["OpenAIKey"];
                model = ConfigurationManager.AppSettings["OpenAIModel"];
                if (string.IsNullOrEmpty(model))
                    model = "gpt-4o-mini";
            }
            else
            {
                Console.WriteLine("Unknown LLMProvider \"{0}\" in .config file, use \"Claude\" or \"ChatGPT\".", provider);
                return;
            }

            Console.WriteLine("Opening local database {0}", ConfigurationManager.AppSettings["Database"]);

            using (var db = new LiteDatabase(ConfigurationManager.AppSettings["Database"]))
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("No {0} API key specified in .config file, running without translation service.", provider);
                    OffLineMode = true;
                }
                Console.WriteLine("Input: {0}", InputFile);
                if (!OffLineMode)
                    Console.WriteLine("Translating with {0} (model {1})", provider, model);

                var col = db.GetCollection<Translation>("translation");
                ProcessXLFFile(InputFile, ProductName, col, ShowOutput, OffLineMode);
            }
            if (!ShowOutput)
                Console.WriteLine("Processed:{0} Pulled from database:{1} Translated by {2}:{3} Errors:{4}", TranslateCount, TranslateLocalDb, provider, TranslateLlm, TranslateErrors);
        }
        void ProcessXLFFile(string InputFile, string AppName, ILiteCollection<Translation> col, bool ShowOutput, bool OffLineMode)
        {
            string xml = System.IO.File.ReadAllText(InputFile);
            var model2 = XlfParser.Converter.Deserialize(xml);

            Dictionary<string, List<TransUnit>> Translations = new Dictionary<string, List<TransUnit>>();
            for (int i = 0; i < languages.Length; i++)
            {
                Translations[languages[i]] = new List<TransUnit>();
            }
            foreach (var Entry in model2.File.Body.Group.TransUnit)
            {
                string[] results;

                results = TranslateLocal(AppName, Entry.Source, languages, col, OffLineMode);
                if (ShowOutput)
                    UpdateStatus();
                for (int i = 0; i < results.Length; i++)
                {
                    TransUnit tu = new TransUnit
                    {
                        Id = Entry.Id,
                        Note = Entry.Note,
                        Source = Entry.Source,
                        Target = results[i]
                    };
                    Translations[languages[i]].Add(tu);
                }
            }

            for (int i = 0; i < languages.Length; i++)
            {
                string outputLanguage = languages[i].Split('-')[0];
                string outputCountry = languages[i].Split('-')[1];
                Xliff xliffout = new XlfParser.Model.Xliff()
                {
                    Version = 1.2m,
                    File = new XlfParser.Model.File()
                    {
                        Datatype = "xml",
                        SourceLanguage = "en-US",
                        TargetLanguage = outputLanguage + "-" + outputCountry,
                        ToolId = "hougaard.com",
                        Header = new XlfParser.Model.Header()
                        {
                            Tool = new XlfParser.Model.Tool()
                            {
                                Id = "hougaard.com",
                                Company = "hougaard.com",
                                Name = "Erik Hougaard"
                            }
                        },
                        Body = new XlfParser.Model.Body()
                        {
                            Group = new Group()
                            {
                                TransUnit = Translations[languages[i]]
                            }
                        }
                    }
                };

                var xml2 = XlfParser.Converter.Serialize(xliffout);
                System.IO.File.WriteAllText(InputFile.Replace(".g.", ".g." + languages[i] + "."), xml2.Replace("utf-16", "utf-8"));
            }
        }
        string[] TranslateLocal(string AppName, string Txt, string[] outputLanguages, ILiteCollection<Translation> col, bool OffLineMode)
        {
            TranslateCount++;
            string[] result = new string[outputLanguages.Length];
            Dictionary<string, string> ResultList = new Dictionary<string, string>();
            List<string> Missing = new List<string>();
            foreach (var lng in outputLanguages)
            {
                var res = col.Find(x => x.Index == Translation.Hash(lng, Txt));
                if (res.Count() == 0)
                    Missing.Add(lng);
                else
                {
                    ResultList.Add(lng, res.First().target);
                    TranslateLocalDb++;
                }
            }
            if (Missing.Count > 0 && !OffLineMode)
            {
                var llmResult = CallLlm(Txt, Missing.ToArray());
                foreach (var lng in Missing)
                {
                    if (llmResult.ContainsKey(lng))
                    {
                        var translation = new Translation();
                        translation.source = Txt;
                        translation.target = llmResult[lng];
                        translation.Language = lng;
                        translation.Index = Translation.Hash(translation.Language, translation.source);
                        translation.Origin = AppName;
                        col.Insert(translation);
                        col.EnsureIndex(x => x.Index);
                        TranslateLlm++;
                        ResultList.Add(lng, llmResult[lng]);
                    }
                    else
                        ResultList.Add(lng, Txt);
                }
            }
            else
            {
                foreach (var lng in Missing)
                    ResultList.Add(lng, Txt);
            }
            for (int i = 0; i < outputLanguages.Length; i++)
                result[i] = ResultList[outputLanguages[i]];
            return result;
        }
        Dictionary<string, string> CallLlm(string Txt, string[] lang)
        {
            string prompt = BuildPrompt(Txt, lang);
            int Retries = 0;
            do
            {
                try
                {
                    string responseText;
                    if (provider == "Claude")
                        responseText = CallClaude(prompt);
                    else
                        responseText = CallChatGpt(prompt);

                    var parsed = ParseJsonResult(responseText);
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    foreach (var lng in lang)
                    {
                        if (parsed.ContainsKey(lng) && !string.IsNullOrEmpty(parsed[lng]))
                            result[lng] = parsed[lng];
                    }
                    if (result.Count != lang.Length)
                        throw new Exception("LLM response missing one or more languages");
                    return result;
                }
                catch (Exception ex)
                {
                    // Translation failed
                    Thread.Sleep(1000);
                    Retries++;
                    if (Retries > 5)
                    {
                        Console.WriteLine("\nTranslation failed for {0} with error {1} {2}", Txt, ex.Message, ex.StackTrace);
                        TranslateErrors++;
                        Dictionary<string, string> result = new Dictionary<string, string>();
                        foreach (var lng in lang)
                        {
                            if (!result.ContainsKey(lng))
                                result.Add(lng, Txt);
                        }
                        return result;
                    }
                }
            } while (true);
        }
        string BuildSystemPrompt()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("You are a professional software localization translator for Microsoft Dynamics 365 Business Central applications.");
            if (!string.IsNullOrEmpty(appContext))
            {
                sb.AppendLine("Context about the application being translated:");
                sb.AppendLine(appContext);
            }
            sb.AppendLine("Translate the English (en-US) user interface text given by the user into each of the target locales listed.");
            sb.AppendLine("Rules:");
            sb.AppendLine("- Use the application context above to pick the terminology and translation that best fits the domain.");
            sb.AppendLine("- Preserve any placeholders such as %1, %2, {0}, {1} exactly as-is.");
            sb.AppendLine("- Keep the same capitalization style and punctuation as the source where appropriate for the target language.");
            sb.AppendLine("- Treat the locale \"es-ES_tradnl\" as Spanish (Spain, traditional sort).");
            sb.AppendLine("- Respond ONLY with a single JSON object where each key is the exact locale code given and each value is the translated text. No markdown, no explanation.");
            return sb.ToString();
        }
        string BuildPrompt(string Txt, string[] lang)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Target locales: " + string.Join(", ", lang));
            sb.AppendLine();
            sb.AppendLine("Text to translate:");
            sb.Append(Txt);
            return sb.ToString();
        }
        string CallClaude(string prompt)
        {
            var payload = new JObject
            {
                ["model"] = model,
                ["max_tokens"] = 4096,
                ["system"] = BuildSystemPrompt(),
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = prompt
                    }
                }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw new Exception("Claude API error " + (int)response.StatusCode + ": " + body);
            var json = JObject.Parse(body);
            return (string)json["content"][1]["text"];
        }
        string CallChatGpt(string prompt)
        {
            var payload = new JObject
            {
                ["model"] = model,
                ["response_format"] = new JObject { ["type"] = "json_object" },
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "system",
                        ["content"] = BuildSystemPrompt()
                    },
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = prompt
                    }
                }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw new Exception("OpenAI API error " + (int)response.StatusCode + ": " + body);
            var json = JObject.Parse(body);
            return (string)json["choices"][0]["message"]["content"];
        }
        Dictionary<string, string> ParseJsonResult(string responseText)
        {
            string text = responseText.Trim();
            // Strip markdown code fences if the model added them anyway
            if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                if (firstNewline >= 0)
                    text = text.Substring(firstNewline + 1);
                int fenceEnd = text.LastIndexOf("```");
                if (fenceEnd >= 0)
                    text = text.Substring(0, fenceEnd);
                text = text.Trim();
            }
            var json = JObject.Parse(text);
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var prop in json.Properties())
                result[prop.Name] = (string)prop.Value;
            return result;
        }
    }
}
