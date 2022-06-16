using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Translate
    {
        public Translate()
        {

        }
        public Result Run(JObject Settings, string hostFile, Boolean OffLineMode)
        {
            var worker = new TranslationTools.TranslateXlf();
            worker.DoTheWork(Settings["XLFPath"].ToString(), Settings["ProductName"].ToString(), hostFile,true,OffLineMode);
            Console.WriteLine();
            return new Result(true);
        }
    }
}
