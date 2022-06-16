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

            js["version"] = VersionParts[0] + "." + VersionParts[1] + "." + VersionParts[2] + "." + VersionParts[3];
            Console.WriteLine("- New version {0}", js["version"].ToString());


            File.WriteAllText(appjson, js.ToString());

            return new Result(true);
            //if (args[argno].ToLower().Contains(".csproj"))
            //{
            //    var xml = File.ReadAllText(args[argno]);
            //    int p1 = xml.IndexOf("<Version>");
            //    int p2 = xml.IndexOf("</Version>");

            //    var VersionParts = xml.Substring(p1 + "<Version>".Length, p2 - p1 - "<Version>".Length).Split('.');
            //    int v = Convert.ToInt32(VersionParts[PartNo]);
            //    Console.WriteLine("Old Build No {0}", v);
            //    if (NewBuildNumber == 0)
            //    {
            //        NewBuildNumber = v + 1;
            //        Console.WriteLine("New Build No {0}", NewBuildNumber);
            //        File.WriteAllText("SETVERSION.CMD", "SET ALVERSION=" + NewBuildNumber.ToString() + "\n");
            //    }
            //    VersionParts[PartNo] = NewBuildNumber.ToString();
            //    xml = xml.Substring(0, p1 + 9) + VersionParts[0] + "." + VersionParts[1] + "." + VersionParts[2] + "." + VersionParts[3] + xml.Substring(p2);
            //    File.WriteAllText(args[argno], xml);
            //}
        }
    }
}
