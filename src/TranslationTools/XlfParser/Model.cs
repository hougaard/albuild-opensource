using System;
using System.Collections.Generic;
using System.Text;

namespace XlfParser.Model
{
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    [System.Xml.Serialization.XmlRootAttribute(ElementName = "xliff", Namespace = "urn:oasis:names:tc:xliff:document:1.2", IsNullable = false)]
    public class Xliff
    {
        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "version")]
        public decimal Version { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("file", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public File File { get; set; }
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    [System.Xml.Serialization.XmlRootAttribute(ElementName = "file", Namespace = "urn:oasis:names:tc:xliff:document:1.2", IsNullable = false)]
    public class File
    {
        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "datatype")]
        public string Datatype { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "source-language")]
        public string SourceLanguage { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "target-language")]
        public string TargetLanguage { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "tool-id")]
        public string ToolId { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("header", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Header Header { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("body", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Body Body { get; set; }
    }

    #region Header

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Header
    {
        [System.Xml.Serialization.XmlElementAttribute("tool", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Tool Tool { get; set; }
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Tool
    {
        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "tool-id")]
        public string Id { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "tool-name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "tool-company")]
        public string Company { get; set; }
    }

    #endregion

    #region Body

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Body
    {
        [System.Xml.Serialization.XmlElementAttribute("group", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public Group Group { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("trans-unit", Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
        public List<TransUnit> TransUnit { get; set; }
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:oasis:names:tc:xliff:document:1.2")]
    public class Group
    {
        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "datatype")]
        public string Datatype { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("trans-unit", Namespace = "urn:oasis:names:tc:xliff:document:1.2")] 
        public List<TransUnit> TransUnit { get; set; }
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(ElementName = "trans-unit", IsNullable = false)]
    public class TransUnit
    {
        [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "source")]
        public string Source { get; set; }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "target")]
        public string Target { get; set; }

        [System.Xml.Serialization.XmlElementAttribute(ElementName = "note")]
        public string Note { get; set; }
    }

    #endregion
}
