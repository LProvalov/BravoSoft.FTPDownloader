using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using DBDownloader.XML.Models.Autocomplects;
using DBDownloader.XML.Models.Products;

namespace DBDownloader.XML
{
    public class ProductsParser
    {
        private IEnumerable<FileInfo> productFiles;
        private Dictionary<string, PriceList> priceLists;

        public ProductsParser(IEnumerable<FileInfo> productFiles)
        {
            this.productFiles = productFiles;
            priceLists = new Dictionary<string, PriceList>();
            foreach (FileInfo file in productFiles)
            {
                if (file.Exists)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PriceList));
                    StreamReader streamReader = new StreamReader(file.FullName);
                    PriceList pl = serializer.Deserialize(streamReader) as PriceList;
                    priceLists.Add(file.Name, pl);
                    streamReader.Close();
                }
            }
        }

        public IList<Tom> GetDBList(IEnumerable<AutoComplect> productsIds, string productListName)
        {
            List<Tom> toms = new List<Tom>();
            PriceList priceList;
            if (priceLists.TryGetValue(productListName, out priceList))
            {
                IList<Product> productList = priceList.GetAllProducts();
                foreach (AutoComplect ac in productsIds)
                {
                    IEnumerable<Product> products =
                        productList.Where(p => p.Id == ac.Kompl.ToString());
                    foreach (Product product in products)
                    {
                        if(product.Toms != null)
                            toms.AddRange(product.Toms);
                    }
                }
            }
            return toms;
        }
    }
}
