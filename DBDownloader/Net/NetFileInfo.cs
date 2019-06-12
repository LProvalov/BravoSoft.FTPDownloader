using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Net
{
    public class NetFileInfo
    {
        public long Length { get; set; }
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
    }
}
