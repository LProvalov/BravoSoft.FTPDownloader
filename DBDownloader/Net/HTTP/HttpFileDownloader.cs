using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBDownloader.Net.HTTP
{
    public sealed class HttpFileDownloader : NetFileDownloader
    {
        public HttpFileDownloader(FileInfo destinationFileInfo, Uri sourceUri, 
            long sourceSize) : base(destinationFileInfo, sourceUri, sourceSize)
        {

        }

        protected override Task DownloadFileAsync(Uri sourceUri, FileInfo destinationFile)
        {
            throw new NotImplementedException();
        }
    }
}
