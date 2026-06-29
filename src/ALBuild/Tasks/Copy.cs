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
                var path = Settings["From"].ToString();

                // Get files matching the pattern, order by LastWriteTime descending, and select their full paths
                FileInfo? file = new DirectoryInfo(path)
                    .GetFiles(@"*.app")                     // apply wildcard
                    .OrderByDescending(f => f.LastWriteTime)         // oldest first
                    .FirstOrDefault();

                var fileName = file?.FullName;
                if (file != null)
                {
                    Console.WriteLine("Copy {0} to {1}", Settings["From"].ToString(), fileName);
                    File.Copy(fileName, Path.Combine(Settings["To"].ToString(), file.Name), true);
                }
            }
            catch (Exception ex)
            {
                return new Result(false, ex.Message);
            }
            return new Result(true);
        }
    }
}
