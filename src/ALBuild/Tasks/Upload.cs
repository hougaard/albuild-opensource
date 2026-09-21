using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBuild.Tasks
{
    internal class Upload
    {
        public Upload()
        {

        }
        public Result Run(JObject Settings)
        {
            try
            {
                HttpClient client = new HttpClient();

                var ListOfFiles = System.IO.Directory.GetFiles(Settings["Path"].ToString(), Settings["Filter"].ToString(), System.IO.SearchOption.TopDirectoryOnly);

                foreach (var File in ListOfFiles)
                {
                    var FileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(File));
                    FileContent.Headers.Add("Content-Type", "application/octet-stream");
                    var Response = client.PostAsync(Settings["Endpoint"].ToString().Replace("%1", Path.GetFileName(File)), FileContent).Result;
                    if (!Response.IsSuccessStatusCode)
                    {
                        var ResponseContent = Response.Content.ReadAsStringAsync().Result;
                        Console.WriteLine(ResponseContent);
                        return new Result(false, $"   - Failed to upload file {Path.GetFileName(File)}. Status code: {Response.StatusCode}");
                    }
                    else
                    {
                        Console.Write($"   - Uploaded file {Path.GetFileName(File)} - Result: ");
                        var ResponseContent = Response.Content.ReadAsStringAsync().Result;
                        Console.WriteLine(ResponseContent);
                    }
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
