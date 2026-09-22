using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class TranslateLlm
    {
        public TranslateLlm()
        {

        }
        public Result Run(JObject Settings, string hostFile, Boolean OffLineMode)
        {
            var worker = new TranslationTools.TranslateXlfLlm();
            if (Settings["SystemPrompt"] != null)
                worker.SystemPrompt = Settings["SystemPrompt"].ToString();
            if (Settings["StripPrefixes"] != null)
                worker.StripPrefixes = Settings["StripPrefixes"].ToString();
            worker.DoTheWork(Settings["XLFPath"].ToString(), Settings["ProductName"].ToString(), hostFile, true, OffLineMode);
            Console.WriteLine();
            return new Result(true);
        }
    }
}
