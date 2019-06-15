using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using DBDownloader.ConfigReader;
using DBDownloader.Net.FTP;

namespace DBDownloader.XML
{
    // Provide *.xml files from ftp server. 
    // Can store files in temp directory on local computer.
    public class FtpFilesProvider
    {
        private NetworkCredential credential;
        private bool useProxy;
        private string proxyAddress;
        private bool usePassiveFtp;

        public FtpFilesProvider(string user, string password, bool useProxy = false, bool usePassiveFTP = true, string proxyAddress = "")
            : this(credential: new NetworkCredential(user, password), useProxy: useProxy, proxyAddress: proxyAddress, usePassiveFTP: usePassiveFTP)
        {
        }

        public FtpFilesProvider(NetworkCredential credential, bool useProxy = false, bool usePassiveFTP = true, string proxyAddress = "")
        {
            this.credential = credential;
            this.useProxy = useProxy;
            this.proxyAddress = proxyAddress;
            this.usePassiveFtp = usePassiveFTP;
        }

        public FileInfo GetFile(string destinationFilePath, string sourcePath)
        {
            FileInfo destinationFile = new FileInfo(destinationFilePath);
            if (destinationFile.Exists) {
                destinationFile.Delete();
                destinationFile.Refresh();
            }
            Uri sourceUri = new Uri(sourcePath);
            FTPDownloader downloader =
                new FTPDownloader(FtpClient.CreateClient(), destinationFile, sourceUri);
            downloader.BeginAsync().Wait();
            destinationFile.Refresh();
            if (destinationFile.Exists) return destinationFile;
            else throw new Exception("Can't download file.");
        }

        public IEnumerable<FileInfo> GetProductsFiles(string destinationDirectory,
            FtpConfiguration configReader)
        {
            List<FileInfo> productFiles = new List<FileInfo>();
            foreach (var productModelItem in configReader.ProductModelItems)
            {
                string productFilePath = string.Format(@"{0}\{1}",
                    destinationDirectory, productModelItem.ProductFileName);
                FileInfo destinationFile = new FileInfo(productFilePath);

                string sourceFilePath = string.Format(@"{0}/{1}",
                    configReader.ProductsPath, productModelItem.ProductFileName);
                Uri sourceUri = new Uri(sourceFilePath);

                if (destinationFile.Exists)
                {
                    destinationFile.Delete();
                    destinationFile.Refresh();
                }
                FTPDownloader downloader =
                    new FTPDownloader(FtpClient.CreateClient(), destinationFile, sourceUri);
                
                downloader.BeginAsync().Wait();
                destinationFile.Refresh();
                if (destinationFile.Exists) productFiles.Add(destinationFile);
            }
            return productFiles;
        }
    }
}
