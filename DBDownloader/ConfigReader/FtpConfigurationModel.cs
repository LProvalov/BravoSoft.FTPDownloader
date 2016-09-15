using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DBDownloader.ConfigReader
{
    [Serializable]
    public class FtpConfigurationModel
    {
        public string FtpSourcePath { get; set; }
        public string DBPath { get; set; }
        public string AutocomplectsPath { get; set; }
        public string ProductsPath { get; set; }

        [XmlElement("ProductFileNames")]
        public List<string> ProductFileNames { get; set; }
        public string ReportsPath { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string ClearFolder { get; set; }
    }
}
