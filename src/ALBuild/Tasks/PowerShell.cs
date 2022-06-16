using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

namespace ALBuild.Tasks
{
    internal class Powershell
    {
        public Powershell()
        {

        }
        public Result Run(JObject Settings)
        {
            Console.WriteLine("- Command {0}", Settings["Command"].ToString());
            PowerShell ps = PowerShell.Create();
            ps.AddCommand(Settings["Command"].ToString());
            foreach(var res in ps.Invoke())
            {
                Console.WriteLine(res.ToString());
            }

            return new Result(ps.HadErrors == false);
        }
    }
}
