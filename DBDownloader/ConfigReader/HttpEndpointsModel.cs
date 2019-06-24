using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.ConfigReader
{
    [Serializable]
    public class HttpEndpointsModel
    {
        public string BaseIp { get; set; }
        public string Login { get; set; }
        public string DBList { get; set; }
        public string Xml { get; set; }
    }
}
