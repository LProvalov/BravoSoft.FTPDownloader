using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DBDownloader.ConfigReader
{
    [Serializable]
    public class ProductVersionModel
    {
        public string ProductFileName { get; set; }
        public string ProductFileNameUI { get; set; }
        public string ProductBasePath { get; set; }
    }
}
