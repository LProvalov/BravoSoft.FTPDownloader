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
        private HttpClient httpClient;
        public HttpFileDownloader(HttpClient httpClient, FileInfo destinationFileInfo, Uri sourceUri, 
            long sourceSize = 0) : base(destinationFileInfo, sourceUri, sourceSize)
        {
            this.httpClient = httpClient;
        }

        protected override Task DownloadFileAsync(Uri sourceUri, FileInfo destinationFile)
        {
            throw new NotImplementedException();
        }
    }
}
