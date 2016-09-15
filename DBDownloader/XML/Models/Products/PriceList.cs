using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Products
{
    [XmlRoot("PriceList")]
    public class PriceList
    {
        [XmlAttribute("date")]
        public DateTime Date { get; set; }

        [XmlElement("Section")]
        public List<Section> Sections { get; set; }

        public IList<Product> GetAllProducts()
        {
            List<Product> ret = new List<Product>();
            foreach(Section section in Sections)
            {
                ret.AddRange(section.GetAllProducts());
            }
            return ret;
        }
    }
}
