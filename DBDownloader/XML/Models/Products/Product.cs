using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Products
{
    [XmlType("Product")]
    public class Product : BaseAttribute
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("DocsInclude")]
        public long DocsInclude { get; set; }

        [XmlAttribute("HaveProtection")]
        public string HaveProtection { get; set; }

        [XmlAttribute("STO")]
        public string STO { get; set; }

        [XmlAttribute("OPO")]
        public string OPO { get; set; }

        [XmlAttribute("AdditionalSupplyOptions")]
        public string AdditionalSupplyOptions { get; set; }

        [XmlArray("Versions")]
        [XmlArrayItem("Version", typeof(Version))]
        public Version[] Versions { get; set; }

        [XmlArray("Toms")]
        [XmlArrayItem("Tom", typeof(Tom))]
        public Tom[] Toms { get; set; }
    }
}
