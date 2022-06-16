using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALBuild.Tasks
{
    internal class DeploySaaS
    {
        public DeploySaaS()
        {

        }
        public async Task<Result> RunAsync(JObject Settings)
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

                string URL2 = "https://api.businesscentral.dynamics.com/v2.0" + 
                               Settings["TenantId"].ToString() + "/" + 
                               Settings["Environment"].ToString() +
                              "/dev/apps?SchemaUpdateMode=" + Settings["SchemaUpdateMode"] + "&DependencyPublishingOption=default";
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", "bearer " + BearerToken);
                MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
                using (Stream stream = File.OpenRead(Settings["AppFile"].ToString()))
                {
                    string fileName = Path.GetFileName(Settings["AppFile"].ToString());
                    Console.Write("- {0} ", fileName);
                    multipartFormDataContent.Add((HttpContent)new StreamContent(stream), fileName, fileName);
                    var result = await httpClient.PostAsync(URL2, multipartFormDataContent);
                    Console.WriteLine(result.StatusCode);
                    return new Result(result.StatusCode == System.Net.HttpStatusCode.OK, result.Content.ReadAsStringAsync().Result);

                }
            }
            return new Result(false,response.Content.ReadAsStringAsync().Result);
        }
    }
}
