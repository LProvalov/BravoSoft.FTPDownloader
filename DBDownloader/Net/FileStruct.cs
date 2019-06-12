using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Net
{
    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public bool IsDirectory;
        public string CreateTime;
        public DateTime CreateDateTime;
        public string Name;
        public double Length;
    }
}
