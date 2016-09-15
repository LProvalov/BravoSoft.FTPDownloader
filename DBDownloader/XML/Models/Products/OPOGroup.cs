using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Products
{
    [XmlType("OPOGroup")]
    public class OPOGroup
    {
        [XmlAttribute("ID")]
        public long Id { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("UpdateFrequency")]
        public string UpdateFrequency { get; set; }
    }
}
