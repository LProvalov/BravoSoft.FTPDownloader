using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using DBDownloader.MainLogger;
using DBDownloader.ConfigReader;

namespace DBDownloader.FTP
{
    public class FtpFileInfo
    {
        public long Length { get; set; }
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class FTPWorker
    {
        private NetworkCredential credential;
        private bool useProxy;
        private string proxyAddress;
        private bool usePassiveFTP;

        private Dictionary<string, FtpFileInfo> filesDict;

        public FTPWorker(NetworkCredential credential)
        {
            this.credential = credential;
            this.useProxy = Configuration.Instance.UseProxy;
            this.proxyAddress = Configuration.Instance.UseProxy ? Configuration.Instance.ProxyAddress : "";
            this.usePassiveFTP = Configuration.Instance.UsePassiveFTP;
            filesDict = new Dictionary<string, FtpFileInfo>();
        }

        private FtpWebRequest CreateRequest(Uri uri, string method)
        {
            FtpWebRequest request = WebRequest.Create(uri) as FtpWebRequest;
            if (useProxy)
            {
                request.Proxy = string.IsNullOrEmpty(proxyAddress) ?
                    new WebProxy() : new WebProxy(proxyAddress);
            }
            request.UsePassive = usePassiveFTP;
            request.Credentials = credential;
            request.Method = method;
            return request;
        }

        public Dictionary<string, FtpFileInfo> GetDBListWithSize(Uri dbDirUri)
        {
            FtpWebResponse response = null;
            try
            {
                Log.WriteInfo("FTPWorker GetDBListWithSize");

                FTPClient _ftpClient = new FTPClient(dbDirUri.Host, credential.UserName, credential.Password);
                if(useProxy) _ftpClient.ProxyAddress = proxyAddress;
                FTPClient.FileStruct[] listDirectory = _ftpClient.ListDirectory(dbDirUri.PathAndQuery);
                foreach(var item in listDirectory)
                {
                    var filePath = string.Format("{0}/{1}", dbDirUri.OriginalString, item.Name);
                    filesDict.Add(filePath, new FtpFileInfo() { Length = (long)item.Length, FileName = item.Name });
                    GetFileDate(filePath);
                }
            }
            catch(WebException wex)
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
            finally
            {
                if (response != null) response.Close();
            }
            return filesDict;
        }

        public void GetFilesDate(string dbDirUri)
        {
            Log.WriteInfo("FTPWorker GetFilesDate, filesCount:{0}", filesDict.Values.Count);
            foreach (var file in filesDict)
            {
                Uri uri = new Uri(file.Key);
                FtpWebRequest request = CreateRequest(uri, WebRequestMethods.Ftp.GetDateTimestamp);
                try
                {
                    using (FtpWebResponse response = request.GetResponse() as FtpWebResponse)
                    {
                        file.Value.LastModified = response.LastModified;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteError("FTPWorker GetFilesDate {0} Error: {1}", file.Key, ex.Message);
                }
            }
        }

        public void GetFileDate(string dbPathUri)
        {
            Uri uri = new Uri(dbPathUri);
            FtpWebRequest request = CreateRequest(uri, WebRequestMethods.Ftp.GetDateTimestamp);
            try
            {
                using (FtpWebResponse response = request.GetResponse() as FtpWebResponse)
                {
                    FtpFileInfo ftpFileInfo = null;
                    if(filesDict.TryGetValue(dbPathUri, out ftpFileInfo))
                    {
                        filesDict[dbPathUri].LastModified = response.LastModified;
                    }                    
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("FTPWorker GetFileDate {0} Error: {1}", dbPathUri, ex.Message);
            }
        }

        public void SendReportToFtp(FileInfo source, string destinationUrl)
        {
            try
            {
                if (source.Exists)
                {
                    FtpWebRequest ftp = (FtpWebRequest)FtpWebRequest.Create(destinationUrl);
                    ftp.Credentials = credential;
                    ftp.UseBinary = false;
                    ftp.KeepAlive = true;
                    ftp.UsePassive = usePassiveFTP;
                    ftp.Method = WebRequestMethods.Ftp.AppendFile;
                    if (useProxy)
                    {
                        ftp.Proxy = string.IsNullOrEmpty(proxyAddress) ?
                            new WebProxy() : new WebProxy(proxyAddress);
                    }

                    FileStream fs = File.OpenRead(source.FullName);
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Close();

                    Stream ftpstream = ftp.GetRequestStream();
                    ftpstream.Write(buffer, 0, buffer.Length);
                    ftpstream.Close();
                }
            }
            catch(WebException wEx)
            {
                string statusDescription = ((FtpWebResponse)wEx.Response).StatusDescription;
                Log.WriteError("Report - Status Description: {0}", statusDescription);
            }
            catch (Exception ex){
                Log.WriteError("Report - error: {0}", ex.Message);
            }
        }
    }
}
