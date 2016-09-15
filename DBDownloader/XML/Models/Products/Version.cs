using System;
using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Products
{
    [XmlType("Version")]
    public class Version
    {
        [XmlAttribute("ID")]
        public long Id { get; set; }

        [XmlAttribute("Size")]
        public long Size { get; set; }

        [XmlAttribute("UpdateWeekNumber")]
        public long UpdateWeekNumber { get; set; }
    }
}
