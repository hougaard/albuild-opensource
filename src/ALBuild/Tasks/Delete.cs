using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Delete
    {
        public Delete()
        {

        }
        public Result Run(JObject Settings)
        {
            try
            {
                var path = Settings["From"].ToString();

                DeleteFilesWildcard(path);
            }
            catch (Exception ex)
            {
                return new Result(false, ex.Message);
            }
            return new Result(true);
        }

        public static (string directory, string pattern) SplitWildcardPath(string wildcardPath)
        {
            string directory = Path.GetDirectoryName(wildcardPath)!;
            string pattern = Path.GetFileName(wildcardPath)!;

            return (directory, pattern);
        }

        public static FileInfo[] GetFilesWildcard(string wildcardPath)
        {
            var (directory, pattern) = SplitWildcardPath(wildcardPath);

            return new DirectoryInfo(directory).GetFiles(pattern);
        }

        public static void DeleteFilesWildcard(string wildcardPath)
        {
            var files = GetFilesWildcard(wildcardPath);

            foreach (var f in files)
                f.Delete();
        }


    }
}
