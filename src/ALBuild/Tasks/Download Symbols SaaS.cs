using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALBuild.Tasks
{
    internal class DownloadSymbolsSaaS
    {
        public DownloadSymbolsSaaS()
        {

        }
        public async Task<Result> RunAsync(JObject Settings)
        {
            try
            {
                string ClientId = Settings["ClientId"].ToString();
                string ClientSecret = Settings["ClientSecret"].ToString();
                string TenantId = Settings["TenantId"].ToString();

                string URL = "https://login.microsoftonline.com/" + TenantId + "/oauth2/v2.0/token";

                HttpClient client = new HttpClient();
                var content = new StringContent("grant_type=client_credentials" +
                                                "&scope=https://api.businesscentral.dynamics.com/.default" +
                                                "&client_id=" + HttpUtility.UrlEncode(ClientId) +
                                                "&client_secret=" + HttpUtility.UrlEncode(ClientSecret));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                var response = await client.PostAsync(URL, content);
                if (response.IsSuccessStatusCode)
                {
                    JObject Result = JObject.Parse(await response.Content.ReadAsStringAsync());
                    string BearerToken = Result["access_token"].ToString();

                    JObject appjson = JObject.Parse(File.ReadAllText(Settings["AppPath"].ToString() + "\\app.json"));

                    List<string> Apps = new List<string>();
                    List<string> Publishers = new List<string>();
                    List<string> Versions = new List<string>();

                    Apps.Add("Base Application");
                    Publishers.Add("Microsoft");
                    Versions.Add(appjson["application"].ToString());

                    Apps.Add("System Application");
                    Publishers.Add("Microsoft");
                    Versions.Add(appjson["application"].ToString());

                    Apps.Add("Application");
                    Publishers.Add("Microsoft");
                    Versions.Add(appjson["platform"].ToString());

                    Apps.Add("System");
                    Publishers.Add("Microsoft");
                    Versions.Add(appjson["platform"].ToString());

                    JArray deps = (JArray)appjson["dependencies"];

                    foreach (JObject dep in deps)
                    {
                        Apps.Add(dep["name"].ToString());
                        Publishers.Add(dep["publisher"].ToString());
                        Versions.Add(dep["version"].ToString());
                    }

                    for (int i = 0; i < Apps.Count; i++)
                    {
                        Console.WriteLine("- Downloading {0}", Apps[i].ToString());

                        string URL2 = "https://api.businesscentral.dynamics.com/v2.0/" +
                                      Settings["Environment"] +
                                      "/dev/packages?publisher=" + Publishers[i] +
                                      "&appName=" + Apps[i] +
                                      "&versionText=" + Versions[i];

                        HttpClient httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("Authorization", "bearer " + BearerToken);
                        MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();

                        var result = await httpClient.GetByteArrayAsync(URL2);
                        File.WriteAllBytes(Settings["AppPath"].ToString() + "\\.alpackages\\" + Publishers[i] + "_" + Apps[i] + "_" + Versions[i] + "_temp.app", result);
                    }
                }
                return new Result(false, response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Download Failed: {0}", ex.Message);
                return new Result(false, ex.Message);
            }
        }
    }
}
