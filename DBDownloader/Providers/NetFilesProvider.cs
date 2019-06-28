using System;
using System.Collections.Generic;
using System.IO;

using DBDownloader.ConfigReader;
using DBDownloader.Net;
using DBDownloader.Net.FTP;
using DBDownloader.Net.HTTP;
using DBDownloader.Providers;
using static DBDownloader.Net.NetFileDownloader;

namespace DBDownloader.XML
{
    // Provide *.xml files from ftp server. 
    // Can store files in temp directory on local computer.
    public class NetFilesProvider
    {
        private INetClient netClient;

        public NetFilesProvider()
        {
            if (Configuration.Instance.NetClientType == NetClientTypes.FTP) netClient = FtpClient.CreateClient();
            else netClient = HttpClient.CreateClient();
        }

        public FileInfo GetFile(string destinationFilePath, string sourcePath)
        {
            FileInfo destinationFile = new FileInfo(destinationFilePath);
            if (destinationFile.Exists) {
                destinationFile.Delete();
                destinationFile.Refresh();
            }
            Uri sourceUri = netClient.GetSourceUri(sourcePath);
            NetFileDownloader fileDownloader = NetFileDownloader.CreateFileDownloader(
                netClient, destinationFile, sourceUri);
            fileDownloader.BeginAsync().Wait();
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
                    FilesPathProvider.GetProductFilesDirPath(), productModelItem.ProductFileName);
                Uri sourceUri = new Uri(sourceFilePath);

                if (destinationFile.Exists)
                {
                    destinationFile.Delete();
                    destinationFile.Refresh();
                }
                NetFileDownloader fileDownloader = NetFileDownloader.CreateFileDownloader(
                    netClient, destinationFile, sourceUri);

                fileDownloader.BeginAsync().Wait();
                destinationFile.Refresh();
                if (destinationFile.Exists) productFiles.Add(destinationFile);
            }
            return productFiles;
        }
    }
}
