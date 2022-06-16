using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Sign
    {
        public Sign()
        {

        }
        public Result Run(JObject Settings,JObject CurrentApp,string hostFile)
        {
            string SignToolExePath = hostFile.Replace("ALBuild.exe", "signtool.exe");

            string AppName = CurrentApp["publisher"] + "_" + CurrentApp["name"] + "_" + CurrentApp["version"] + ".app";

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SignToolExePath,
                    Arguments = "sign " +
                                "/f \"" + Settings["KeyFile"] + "\" " +
                                "/p \"" + Settings["Password"] + "\" " +
                                "\"" + Settings["AppPath"] + "\\" + AppName + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                Console.WriteLine(proc.StandardOutput.ReadLine());
            }
          
            return new Result(proc.ExitCode == 0);
        }
    }
}
