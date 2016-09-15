using System.Collections.Generic;
using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Autocomplects
{
    [XmlRoot("DocumentElement")]
    public class AutoComplectList
    {
        [XmlElement("Date")]
        public string Date { get; set; }

        [XmlElement("AutoComplects")]
        public List<AutoComplect> AutoComplects { get; set; }
    }
}
