using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALBuild.Tasks
{
    internal class TestBasicDocker
    {
        public TestBasicDocker()
        {

        }
        public async Task<Result> RunAsync(JObject Settings)
        {
            string BaseURL = Settings["BaseURL"].ToString();
            string User = Settings["User"].ToString();
            string Password = Settings["Password"].ToString();
            string Authentication = Convert.ToBase64String(Encoding.UTF8.GetBytes(User + ":" + Password));

            Console.WriteLine("Calling Test codeunit {0} in company {1}",Settings["TestCodeunit"].ToString(),Settings["Company"].ToString());

            string URL2 = BaseURL.TrimEnd('/') + "/ODataV4/ALBuild_runcodeunit?tenant=default&company=" + Settings["Company"];
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Authentication);
            string input = "{\"no\":" + Settings["TestCodeunit"].ToString() + "}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync(URL2, content);
            //Console.WriteLine(result.StatusCode);
            return new Result(result.StatusCode == System.Net.HttpStatusCode.OK, result.Content.ReadAsStringAsync().Result);
        }
    }
}
