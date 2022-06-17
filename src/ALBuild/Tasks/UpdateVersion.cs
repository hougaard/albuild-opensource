using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class UpdateVersion
    {
        public UpdateVersion()
        {

        }
        public Result Run(JObject Settings)
        {
            int NewBuildNumber = 0;
            var appjson = Path.Combine(Settings["AppPath"].ToString(), "app.json");
            int PartNo = Settings["VersionPartToIncrement"].ToObject<int>() - 1;
            int Increment = Settings["Increment"].ToObject<int>();

            Console.WriteLine("- Updating {0}", appjson);
            var appTxt = File.ReadAllText(appjson);
            var js = JObject.Parse(appTxt);
            var version = js.GetValue("version").ToString();

            Console.WriteLine("- Old version {0}", version);

            var VersionParts = version.Split('.');
            int v = Convert.ToInt32(VersionParts[PartNo]);
            if (NewBuildNumber == 0)
            {
                NewBuildNumber = v + 1;
            }
            VersionParts[PartNo] = NewBuildNumber.ToString();

            if (Settings.ContainsKey("DateInVersionPartNo"))
            {
                var DatePartNo = Settings["DateInVersionPartNo"].ToObject<int>() - 1;
                VersionParts[DatePartNo] = DateTime.Now.ToString("yyyymmdd");
            }

            js["version"] = VersionParts[0] + "." + VersionParts[1] + "." + VersionParts[2] + "." + VersionParts[3];
            Console.WriteLine("- New version {0}", js["version"].ToString());

            File.WriteAllText(appjson, js.ToString());

            return new Result(true);
        }
    }
}
