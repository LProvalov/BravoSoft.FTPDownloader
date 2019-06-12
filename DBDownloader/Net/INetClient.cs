using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Net
{
    public interface INetClient
    {
        FileStruct[] ListDirectory(string path);
        IEnumerable<FileStruct> FillCreateDateTime(string path, FileStruct[] filestructs);
    }
}
