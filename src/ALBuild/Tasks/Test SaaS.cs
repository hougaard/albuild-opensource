using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALBuild.Tasks
{
    internal class TestSaaS
    {
        public TestSaaS()
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

                Console.WriteLine("Calling Test codeunit {0} in company {1}", Settings["TestCodeunit"].ToString(), Settings["Company"].ToString());

                string URL2 = "https://api.businesscentral.dynamics.com/v2.0/" + TenantId + "/" + Settings["Environment"] + "/ODataV4/ALBuild_runcodeunit?company=" + Settings["Company"];
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", "bearer " + BearerToken);
                string input = "{\"no\":" + Settings["TestCodeunit"].ToString() + "}";
                var content2 = new StringContent(input, Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(URL2, content2);
                //Console.WriteLine(result.StatusCode);
                return new Result(result.StatusCode == System.Net.HttpStatusCode.OK, result.Content.ReadAsStringAsync().Result);
            }
            return new Result(false,response.Content.ReadAsStringAsync().Result);
        }
    }
}
