using DBDownloader.ConfigReader;
using DBDownloader.MainLogger;
using DBDownloader.Net;
using DBDownloader.Net.FTP;
using DBDownloader.Net.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using static DBDownloader.Net.NetFileDownloader;

namespace DBDownloader.Providers
{
    public class DataProvider
    {
        private INetClient netClient;
        private string LOG_TAG = "DataProvider";

        public DataProvider()
        {
            if (Configuration.Instance.NetClientType == NetClientTypes.FTP)
                netClient = FtpClient.CreateClient();
            else netClient = HttpClient.CreateClient();
        }

        public Dictionary<string, NetFileInfo> GetDBListWithSize(Uri dbDirUri)
        {
            Dictionary<string, NetFileInfo> filesDict = new Dictionary<string, NetFileInfo>();
            try
            {
                Log.WriteInfo(string.Format("{0} GetDBListWithSize", LOG_TAG));
                FileStruct[] listDirectory = netClient.ListDirectory(dbDirUri.PathAndQuery);
                foreach(var item in listDirectory)
                {
                    var filePath = string.Format("{0}/{1}", dbDirUri.OriginalString, item.Name);
                    NetFileInfo fileInfo = new NetFileInfo() { Length = (long)item.Length, FileName = item.Name };
                    if (netClient is FtpClient)
                    {
                        DateTime? lastModified = (netClient as FtpClient).GetLastModifiedFileDate(filePath);
                        if (lastModified.HasValue) fileInfo.LastModified = lastModified.Value;
                    }
                    if (netClient is HttpClient)
                    {
                        fileInfo.LastModified = item.LastModifiedDateTime;
                    }
                    filesDict.Add(filePath, fileInfo);
                }
            }
            catch (WebException wex)
            {
                string statusDescription = ((FtpWebResponse)wex.Response).StatusDescription;
                Log.WriteError("FTPWorker GetDBListWithSize Web Error: {0}", wex.Message);
                if (wex.InnerException != null) Log.WriteError("Internal Exception: {0}", wex.InnerException.Message);
                Log.WriteError("FTPWorker GetDBListWithSize status description:{0}", statusDescription);
            }
            catch (Exception ex)
            {
                Log.WriteError("FTPWorker GetDBListWithSize Error: {0}", ex.Message);
            }
            return filesDict;
        }

        public void SendReportToServer(FileInfo source, string destinationUrl)
        {
            FtpClient client;
            if (netClient is FtpClient)
            {
                client = netClient as FtpClient;
            } else
            {
                client = FtpClient.CreateClient();
            }

            client.SendReportToServer(source, destinationUrl);
        }

        public FileStruct[] ListDirectory(string pathUri)
        {
            return netClient.ListDirectory(pathUri);
        }
    }
}
