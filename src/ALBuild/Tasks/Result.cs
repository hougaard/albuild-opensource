using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Result
    {
        public Result(bool _success)
        {
            Success = _success;
        }
        public Result(bool _success, string _msg)
        {
            Success = _success;
            Message = _msg;
        }
        public Result(bool _success, JObject data)
        {
            Success= _success;
            Data = data;
        }
        public bool Success { get; set; }
        public string Message { get; set; }
        public JObject Data { get; set; }
    }
}
