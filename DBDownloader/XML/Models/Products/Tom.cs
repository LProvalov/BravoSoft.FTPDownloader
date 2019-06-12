using System;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace DBDownloader.XML.Models.Products
{
    [XmlType("Tom")]
    public class Tom : BaseAttribute
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("FileName")]
        public string FileName { get; set; }
        
        [XmlAttribute("DocsInclude")]
        public long DocsInclude { get; set; }

        [XmlAttribute("HaveProtection")]
        public string HaveProtection { get; set; }

        [XmlAttribute("STO")]
        public string STO { get; set; }

        [XmlArray("Versions")]
        [XmlArrayItem("Version", typeof(Version))]
        public Version[] Versions { get; set; }

        [XmlArray("OPOGroups")]
        [XmlArrayItem("OPOGroup", typeof(OPOGroup))]
        public OPOGroup[] OPOGroups { get; set; }

        [XmlAttribute("FullPathname")]
        public string FullPathname { get; set; }
    }
}
