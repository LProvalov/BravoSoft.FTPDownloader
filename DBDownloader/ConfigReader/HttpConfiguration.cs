using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DBDownloader.ConfigReader
{
    [Serializable]
    public class HttpConfiguration
    {
        public string User { get; set; }
        public string Password { get; set; }
        [XmlElement("HttpEndpoints")]
        public HttpEndpointsModel HttpEndpoints { get; set; }
    }
}
