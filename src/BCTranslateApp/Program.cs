using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;
using XlfParser.Model;
using System.Threading;
using LiteDB;
using TranslationTools;
using Azure.Data.Tables;
using System.IO;
using System.Diagnostics;

namespace BCTranslateApp
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ALBuild - Translate App 22.05.01");
            Console.WriteLine("(c) 2022 Erik Hougaard - hougaard.com");

            if (args.Length != 2)
            {
                Console.WriteLine("Syntax: BCTranslateApp <input extension.g.xlf file> <App Name>");
                return;
            }
            var hostFile = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\BCTranslateApp.exe";

            var Worker = new TranslateXlf();
            Worker.DoTheWork(args[0], args[1], hostFile, true, false);
        }

    }
}