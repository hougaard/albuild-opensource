using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Remember
    {
        public Remember()
        {
        }
        public Result Run(JObject Settings)
        {
            var appjson = Path.Combine(Settings["AppPath"].ToString(), "app.json");
            Console.WriteLine("- Using {0}",appjson);

            var appTxt = File.ReadAllText(appjson);
            var js = JObject.Parse(appTxt);
            js["apppath"] = Settings["AppPath"];

            return new Result(true, js);
        }
    }   
}
