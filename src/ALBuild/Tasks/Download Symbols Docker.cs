using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALBuild.Tasks
{
    internal class DownloadSymbolsDocker
    {
        public DownloadSymbolsDocker()
        {

        }
        public async Task<Result> RunAsync(JObject Settings)
        {
            try
            {
                string BaseURL = Settings["BaseURL"].ToString().TrimEnd('/');
                string User = Settings["User"].ToString();
                string Password = Settings["Password"].ToString();
                string Authentication = Convert.ToBase64String(Encoding.UTF8.GetBytes(User + ":" + Password));

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
                    Console.WriteLine("- Downloading {0}",Apps[i].ToString());
                    string URL2 = BaseURL +
                                  "/dev/packages?publisher=" + Publishers[i] +
                                  "&appName=" + Apps[i] +
                                  "&versionText=" + Versions[i];

                    HttpClient httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Authentication);
                    var result = await httpClient.GetByteArrayAsync(URL2);
                    File.WriteAllBytes(Settings["AppPath"].ToString() + "\\.alpackages\\" + Publishers[i] + "_" + Apps[i] + "_" + Versions[i] + "_temp.app", result);
                }
                return new Result(true);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Download Failed: {0}", ex.Message);
                return new Result(false, ex.Message);
            }
        }
    }
}
