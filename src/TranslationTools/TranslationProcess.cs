using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;
using XlfParser.Model;
using System.Threading;
using LiteDB;
using TranslationTools;
using Azure.Data.Tables;
using System.IO;
using System.Diagnostics;

namespace TranslationTools
{
    public class TranslateXlf
    {
        string[] languages;
        TranslateClient client;
        
        int TranslateCount = 0;
        int TranslateRemote = 0;
        int TranslateAzure = 0;
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
            Console.Write("Processed:{0} Pulled from cloud:{1} Translated:{2} Errors:{3}", TranslateCount, TranslateRemote, TranslateAzure, TranslateErrors);
        }

        public TranslateXlf()
        {

        }
        public void DoTheWork(string InputFile, string ProductName, string ConfigFile, bool ShowOutput, bool OffLineMode)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigFile);
            languages = ConfigurationManager.AppSettings["Languages"].Split(',');

            Console.WriteLine("Opening local database {0}", ConfigurationManager.AppSettings["Database"]);

            using (var db = new LiteDatabase(ConfigurationManager.AppSettings["Database"]))
            {
                if ((ConfigurationManager.AppSettings["AzureKey"] == null) ||
                    (ConfigurationManager.AppSettings["AzureKey"] == ""))
                {
                    Console.WriteLine("No Azure Cognitive Service Key specified in .config file, running without translation service.");
                    OffLineMode = true;
                }
                Console.WriteLine("Input: {0}", InputFile);


                if (!OffLineMode)
                {
                    client = new TranslateClient(new CognitiveServices.Translator.Configuration.CognitiveServicesConfig
                    {
                        SubscriptionKey = ConfigurationManager.AppSettings["AzureKey"],
                        SubscriptionKeyAlternate = ConfigurationManager.AppSettings["AzureKey"],
                        Name = ConfigurationManager.AppSettings["Name"]
                    });
                    var col = db.GetCollection<Translation>("translation");
                    Console.WriteLine("Connecting to Azure storage table {0}", ConfigurationManager.AppSettings["storageaccount"] + ".table.core.windows.net");

                    var tableClient = new TableClient(new Uri("https://" + ConfigurationManager.AppSettings["storageaccount"] + ".table.core.windows.net"),
                                                            "translation",
                                                            new TableSharedKeyCredential(ConfigurationManager.AppSettings["storageaccount"], ConfigurationManager.AppSettings["storageaccountkey"]));
                    ProcessXLFFile(InputFile, ProductName, col, tableClient, ShowOutput);
                }
                else
                {
                    var col = db.GetCollection<Translation>("translation");
                    ProcessXLFFile(InputFile, ProductName, col, null, ShowOutput);
                }
            }
            if (!ShowOutput)
                Console.WriteLine("Processed:{0} Pulled from cloud:{1} Translated:{2} Errors:{3}", TranslateCount, TranslateRemote, TranslateAzure, TranslateErrors);
            //Console.WriteLine("\nDone!", db);
            //Console.ReadLine();
        }
        void ProcessXLFFile(string InputFile, string AppName, ILiteCollection<Translation> col, TableClient tc, bool ShowOutput)
        {
            string xml = System.IO.File.ReadAllText(InputFile);
            var model = XlfParser.Converter.Deserialize(xml);

            Dictionary<string, List<TransUnit>> Translations = new Dictionary<string, List<TransUnit>>();
            for (int i = 0; i < languages.Length; i++)
            {
                Translations[languages[i]] = new List<TransUnit>();
            }
            foreach (var Entry in model.File.Body.Group.TransUnit)
            {
                string[] results;

                results = TranslateLocal(AppName, Entry.Source, languages, col, tc);
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
        string[] TranslateLocal(string AppName, string Txt, string[] outputLanguages, ILiteCollection<Translation> col, TableClient tc)
        {
            TranslateCount++;
            string[] result = new string[outputLanguages.Length];
            Dictionary<string, string> ResultList = new Dictionary<string, string>();
            List<string> Missing = new List<string>();
            foreach (var lng in outputLanguages)
            {
                var res = col.Find(x => x.Index == Translation.Hash(lng, Txt));
                if (res.Count() == 0)
                {

                    try
                    {
                        var res2 = tc.GetEntity<TranslationTableEntry>(lng, Translation.Hash(lng, Txt), null, CancellationToken.None);
                        ResultList.Add(lng, res2.Value.Target);

                        var translation = new Translation();
                        translation.source = Txt;
                        translation.target = res2.Value.Target;
                        translation.Language = lng;
                        translation.Index = Translation.Hash(translation.Language, translation.source);
                        translation.Origin = AppName;
                        col.Insert(translation);
                        col.EnsureIndex(x => x.Index);
                        TranslateRemote++;
                    }
                    catch
                    {
                        Missing.Add(lng);
                    }
                }
                else
                    ResultList.Add(lng, res.First().target);
            }
            if (Missing.Count > 0 && tc != null)
            {
                string[] lang = new string[Missing.Count];
                for (int i = 0; i < Missing.Count; i++)
                    lang[i] = Missing[i].Split('-')[0];
                TranslateAzure++;
                var webserviceresult = CallWebService(Txt, lang);
                for (int i = 0; i < outputLanguages.Length; i++)
                {
                    if (webserviceresult.ContainsKey(outputLanguages[i].Split('-')[0]))
                    {
                        var translation = new Translation();
                        translation.source = Txt;
                        translation.target = webserviceresult[outputLanguages[i].Split('-')[0]];
                        translation.Language = outputLanguages[i];
                        translation.Index = Translation.Hash(translation.Language, translation.source);
                        translation.Origin = AppName;
                        col.Insert(translation);
                        col.EnsureIndex(x => x.Index);
                        result[i] = webserviceresult[outputLanguages[i].Split('-')[0]];

                        var entity = new TableEntity(translation.Language, translation.Index)
                                                    {
                                                        { "Language", translation.Language},
                                                        { "Origin", translation.Origin },
                                                        { "Source", translation.source },
                                                        { "Target", translation.target }
                                                    };
                        try
                        {
                            tc.AddEntity(entity);
                            TranslateAzure++;
                        }
                        catch (Exception ex)
                        {
                            tc.UpdateEntity(entity, Azure.ETag.All);
                            //TranslateErrors++;
                            //Console.WriteLine("\nCould not update Azure Table, error: {0}\n", ex.Message);
                        }
                    }
                    else
                        result[i] = ResultList[outputLanguages[i]];
                }
                return result;
            }
            else
            {
                for (int i = 0; i < outputLanguages.Length; i++)
                    result[i] = ResultList[outputLanguages[i]];
                return result;
            }
        }
        Dictionary<string, string> CallWebService(string Txt, string[] lang)
        {
            string[] outputLanguages = new string[lang.Length];
            RequestContent rc = new RequestContent()
            {
                Text = Txt
            };
            RequestParameter rp = new RequestParameter
            {
                From = "en",
                To = lang
            };
            int Retries = 0;
            Boolean Translated = false;
            do
            {
                try
                {
                    var rb = client.Translate(rc, rp);

                    //Console.Write(".");
                    //string[] result = new string[lang.Length];
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    for (int i = 0; i < lang.Length; i++)
                    {
                        if (!result.ContainsKey(lang[i]))
                            result.Add(lang[i], rb[0].Translations[i].Text);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    // Translation failed
                    Thread.Sleep(10000);
                    Retries++;
                    if (Retries > 5)
                    {
                        Console.WriteLine("\nTranslation failed for {0} with error {1}", Txt, ex.Message);
                        TranslateErrors++;
                        //string[] result = new string[outputLanguages.Length];
                        Dictionary<string, string> result = new Dictionary<string, string>();
                        for (int i = 0; i < lang.Length; i++)
                        {
                            if (!result.ContainsKey(lang[i]))
                                result.Add(lang[i], Txt);
                        }
                        return result;
                    }
                }
            } while (!Translated);
            return null;
        }
    }

}