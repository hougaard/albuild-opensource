using Newtonsoft.Json.Linq;
using System;
using ALBuild.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ALBuild
{
    internal class Program
    {
        static Boolean OffLineMode = false;
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("ALBuild 22.05.01");
            Console.WriteLine("(c) 2022 Erik Hougaard - hougaard.com");
            if (args.Length == 0)
            {
                Console.WriteLine("Syntax: ALBuild <buildscript json file> [-offline]");
                return;
            }

            var hostFile = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\ALBuild.exe";


            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Cannot find {0}", args[0]);
                return;
            }
            Console.WriteLine("- Reading build script {0}", args[0]);
                var BuildScript = JObject.Parse(File.ReadAllText(args[0]));
            Console.WriteLine("- Building {0}", BuildScript["Project"]);

            if (args.Length > 1)
            {
                for(int i = 1; i < args.Length; i++)
                {
                    if (args[i].ToLower() == "-offline")
                        OffLineMode = true;
                }
            }
            var StartTime = DateTime.Now;
            JArray Tasks = (JArray)BuildScript["Tasks"];
            JObject CurrentApp = null;
            foreach (var Task in Tasks)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("- {0}",Task["Type"].ToString());
                Console.ForegroundColor = ConsoleColor.Yellow;
                PrepareSettings(Task, CurrentApp);
                Result Res = null;
                switch(Task["Type"].ToString())
                {
                    case "Git":
                        Res = new Git().Run((JObject)Task["Settings"],OffLineMode);
                        break;
                    case "UpdateVersion":
                        Res = new UpdateVersion().Run((JObject)Task["Settings"]);
                        break;
                    case "Remember":
                        Res = new Remember().Run((JObject)Task["Settings"]);
                        CurrentApp = Res.Data;
                        break;
                    case "Compile":
                        Res = new Compile().Run((JObject)Task["Settings"]);
                        break;
                    case "Translate":
                        Res = new Translate().Run((JObject)Task["Settings"],hostFile,OffLineMode);
                        break;
                    case "Sign":
                        Res = new Sign().Run((JObject)Task["Settings"],CurrentApp,hostFile);
                        break;
                    case "Copy":
                        Res = new Copy().Run((JObject)Task["Settings"]);
                        break;
                    case "PowerShell":
                        Res = new Powershell().Run((JObject)Task["Settings"]);
                        break;
                    case "DeploySaaS":
                        Res = await new DeploySaaS().RunAsync((JObject)Task["Settings"]);
                        break;
                    case "DeployBasicDocker":
                        Res = await new DeployBasicDocker().RunAsync((JObject)Task["Settings"]);
                        break;
                    case "TestSaaS":
                        Res = await new TestSaaS().RunAsync((JObject)Task["Settings"]);
                        break;
                    case "TestBasicDocker":
                        Res = await new TestBasicDocker().RunAsync((JObject)Task["Settings"]);
                        break;
                    case "DownloadSymbolsSaaS":
                        Res = await new DownloadSymbolsSaaS().RunAsync((JObject)Task["Settings"]);
                        break;
                    case "DownloadSymbolsDocker":
                        Res = await new DownloadSymbolsDocker().RunAsync((JObject)Task["Settings"]);
                        break;
                    default:
                        Console.WriteLine("Unkown task \"{0}\", aborting", Task["Type"].ToString());
                        return;
                }
                if (Res != null)
                {
                    if (Res.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Completed Successful");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: {0}",Res.Message);
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                }
            }

            Console.WriteLine("- Build completed, time elapsed {0}, existing", DateTime.Now - StartTime);
            //Console.ReadLine();
        }

        private static void PrepareSettings(JToken task, JObject? currentApp)
        {
            Regex reg = new Regex(@"\%([A-Z0-9]*)\%");
            if (currentApp != null)
            {
                var Settings = (JObject)task["Settings"];
                foreach(var setting in Settings)
                {
                    while (reg.IsMatch(Settings[setting.Key].ToString()))
                    {
                            var m = reg.Match(Settings[setting.Key].ToString());
                        if (m.Success)
                        {
                            Capture cap = m.Captures[0];
                            var SettingsKey = Settings[setting.Key].ToString().Substring(cap.Index, cap.Length);
                            var newvalue = currentApp[SettingsKey.Trim('%').ToLower()].ToString();
                            Settings[setting.Key] = Settings[setting.Key].ToString().Replace(SettingsKey,newvalue);

                        }
                    }
                }
            }
        }
    }
}