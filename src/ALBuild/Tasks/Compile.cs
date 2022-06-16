using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Compile
    {
        public Compile()
        {

        }
        public Result Run(JObject Settings)
        {
            //Console.WriteLine()
            var CompilerPath = LocateCompilerFolder();
            
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = CompilerPath + "\\bin\\alc.exe",
                    Arguments = "/project:\"" + Settings["AppPath"].ToString() + 
                                "\" /packagecachepath:\"" + Settings["AppPath"].ToString() + "\\.alpackages",
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
        public string LocateCompilerFolder()
        {
            foreach (var folder in Directory.GetDirectories(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.vscode\\extensions\\")))
            {
                if (folder.Contains("ms-dynamics-smb.al"))
                {
                    return folder;
                }
            }
            throw new Exception("Cannot locate Business Central ALC Compiler, cannot continue");
        }
    }
}
