using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using XlfParser.Model;

namespace XlfParser
{
    public static class Converter
    {
        public static Xliff Deserialize(string input)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Xliff));

            using (StringReader sr = new StringReader(input))
            {
                return (Xliff)ser.Deserialize(sr);
            }
        }

        public static string Serialize(Xliff ObjectToSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(ObjectToSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, ObjectToSerialize);
                return textWriter.ToString();
            }
        }
    }
}
