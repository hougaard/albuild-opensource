using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALBuild.Tasks
{
    internal class DeployBasicDocker
    {
        public DeployBasicDocker()
        {

        }
        public async Task<Result> RunAsync(JObject Settings)
        {
            string BaseURL = Settings["BaseURL"].ToString();
            string User = Settings["User"].ToString();
            string Password = Settings["Password"].ToString();
            string Authentication = Convert.ToBase64String(Encoding.UTF8.GetBytes(User + ":" + Password));


            // http://bc19:7049/BC/dev/apps?tenant=default&SchemaUpdateMode=forcesync&DependencyPublishingOption=default

            string URL2 = BaseURL.TrimEnd('/') + "/dev/apps?SchemaUpdateMode=" + Settings["SchemaUpdateMode"] + "&DependencyPublishingOption=default";
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Authentication);
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            using (Stream stream = File.OpenRead(Settings["AppFile"].ToString()))
            {
                string fileName = Path.GetFileName(Settings["AppFile"].ToString());
                Console.Write("- {0} ", fileName);
                multipartFormDataContent.Add((HttpContent)new StreamContent(stream), fileName, fileName);
                var result = await httpClient.PostAsync(URL2, multipartFormDataContent);
                Console.WriteLine(result.StatusCode);
                if (result.Content.ReadAsStringAsync().Result.Contains("A duplicate package ID is detected."))
                    return new Result(true, "Same exact app is already installed");
                else
                    return new Result(result.StatusCode == System.Net.HttpStatusCode.OK, result.Content.ReadAsStringAsync().Result);
            }
        }
    }
}
