using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Git
    {
        public Git()
        {

        }
        public Result Run(JObject Settings, Boolean OffLineMode)
        {
            if (!ExistsOnPath("git.exe"))
                throw new Exception("Cannot find git.exe on this machine");
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetFullPath("git.exe"),
                    WorkingDirectory = Settings["Path"].ToString(),
                    Arguments = Settings["Command"].ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                }
            };
            Console.WriteLine("{0} in {1}", Settings["Command"].ToString(), Settings["Path"].ToString());
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                Console.WriteLine(proc.StandardOutput.ReadLine());
            }
            while (!proc.StandardError.EndOfStream)
            {
                Console.WriteLine(proc.StandardError.ReadLine());
            }
            if (OffLineMode)
            {
                if(proc.ExitCode != 0)
                {
                    Console.WriteLine("Error occured, ignored due to ");
                }
                return new Result(true);
            }
            else
                return new Result(proc.ExitCode == 0);

        }
        public bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
