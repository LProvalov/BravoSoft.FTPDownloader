using System;
using System.Xml.Serialization;

namespace DBDownloader.XML.Models
{
    public abstract class BaseAttribute
    {
        [XmlAttribute("ID")]
        public string Id { get; set; }
    }
}
