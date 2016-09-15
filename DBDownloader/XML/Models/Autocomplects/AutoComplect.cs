using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Autocomplects
{
    [XmlType("AutoComplects")]
    public class AutoComplect
    {
        [XmlElement("REF")]
        public long Ref { get; set; }

        [XmlElement("KOMPL")]
        public long Kompl { get; set; }
    }
}
