using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Net.HTTP
{
    public class HttpClient : INetClient
    {
        public IEnumerable<FileStruct> FillCreateDateTime(string path, FileStruct[] filestructs)
        {
            throw new NotImplementedException();
        }

        public long GetSourceFileSize(Uri sourceUri)
        {
            throw new NotImplementedException();
        }

        public FileStruct[] ListDirectory(string path)
        {
            throw new NotImplementedException();
        }
    }
}
