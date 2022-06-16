using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Copy
    {
        public Copy()
        {

        }
        public Result Run(JObject Settings)
        {
            try
            {
                Console.WriteLine("Copy {0} to {1}", Settings["From"].ToString(), Settings["To"].ToString());
                File.Copy(Settings["From"].ToString(), Settings["To"].ToString());
            }
            catch (Exception ex)
            {
                return new Result(false, ex.Message);
            }
            return new Result(true);
        }
    }
}
