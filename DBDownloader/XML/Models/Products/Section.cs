using System.Collections.Generic;
using System.Xml.Serialization;

namespace DBDownloader.XML.Models.Products
{
    [XmlType("Section")]
    public class Section : BaseAttribute
    {
        [XmlAttribute("Caption")]
        public string Caption { get; set; }

        [XmlArray("Products", IsNullable = true)]
        [XmlArrayItem("Product", typeof(Product))]
        public Product[] Products { get; set; }

        [XmlArray("Sections", IsNullable = true)]
        [XmlArrayItem("Section", typeof(Section))]
        public Section[] Sections { get; set; }

        public IList<Product> GetAllProducts()
        {
            List<Product> ret = new List<Product>();
            if (Products != null) ret.AddRange(Products);
            if (Sections != null)
                foreach (Section section in Sections)
                {
                    ret.AddRange(section.GetAllProducts());
                }
            return ret;
        }
    }
}
