using DBDownloader.ConfigReader;
using DBDownloader.MainLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace DBDownloader.Net.FTP
{
    public class FtpClient : INetClient
    {
        public enum DownloadingStatus
        {
            InProgress,
            Stopping,
            Stopped,
            Done,
            ErrorOccurred,
            WebErrorOccured
        }

        public enum FileListStyle
        {
            UnixStyle,
            WindowsStyle,
            Unknown
        }

        protected class DirectoryListParser
        {
            private List<FileStruct> _myListArray;

            public FileStruct[] FullListing
            {
                get
                {
                    return _myListArray.ToArray();
                }
            }

            public FileStruct[] FileList
            {
                get
                {
                    List<FileStruct> _fileList = new List<FileStruct>();
                    foreach (FileStruct thisstruct in _myListArray)
                    {
                        if (!thisstruct.IsDirectory)
                        {
                            _fileList.Add(thisstruct);
                        }
                    }
                    return _fileList.ToArray();
                }
            }

            public FileStruct[] DirectoryList
            {
                get
                {
                    List<FileStruct> _dirList = new List<FileStruct>();
                    foreach (FileStruct thisstruct in _myListArray)
                    {
                        if (thisstruct.IsDirectory)
                        {
                            _dirList.Add(thisstruct);
                        }
                    }
                    return _dirList.ToArray();
                }
            }

            public DirectoryListParser(string responseString)
            {
                _myListArray = GetList(responseString);
            }

            private List<FileStruct> GetList(string datastring)
            {
                List<FileStruct> myListArray = new List<FileStruct>();
                string[] dataRecords = datastring.Split('\n');
                //Получаем стиль записей на сервере
                FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
                foreach (string s in dataRecords)
                {
                    if (_directoryListStyle != FileListStyle.Unknown && s != "")
                    {
                        FileStruct f = new FileStruct();
                        f.Name = "..";
                        switch (_directoryListStyle)
                        {
                            case FileListStyle.UnixStyle:
                                f = ParseFileStructFromUnixStyleRecord(s);
                                break;
                            case FileListStyle.WindowsStyle:
                                f = ParseFileStructFromWindowsStyleRecord(s);
                                break;
                        }
                        if (f.Name != "" && f.Name != "." && f.Name != "..")
                        {
                            myListArray.Add(f);
                        }
                    }
                }
                return myListArray;
            }
            //Парсинг, если фтп сервера работает на Windows
            private FileStruct ParseFileStructFromWindowsStyleRecord(string Record)
            {
                //Предположим стиль записи 02-03-04  07:46PM       <DIR>     Append
                FileStruct f = new FileStruct();
                string processstr = Record.Trim();
                //Получаем дату
                string dateStr = processstr.Substring(0, 8);
                processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
                //Получаем время
                string timeStr = processstr.Substring(0, 7);
                processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
                f.CreateTime = dateStr + " " + timeStr;
                //Это папка или нет
                if (processstr.Substring(0, 5) == "<DIR>")
                {
                    f.IsDirectory = true;
                    processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
                }
                else
                {
                    string[] strs = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    processstr = strs[1];
                    f.IsDirectory = false;
                }
                //Остальное содержмое строки представляет имя каталога/файла
                f.Name = processstr;
                return f;
            }
            //Получаем на какой ОС работает фтп-сервер - от этого будет зависеть дальнейший парсинг
            public FileListStyle GuessFileListStyle(string[] recordList)
            {
                foreach (string s in recordList)
                {
                    //Если соблюдено условие, то используется стиль Unix
                    if (s.Length > 10
                        && Regex.IsMatch(s.Substring(0, 10), "(-|d)((-|r)(-|w)(-|x)){3}"))
                    {
                        return FileListStyle.UnixStyle;
                    }
                    //Иначе стиль Windows
                    else if (s.Length > 8
                        && Regex.IsMatch(s.Substring(0, 8), "[0-9]{2}-[0-9]{2}-[0-9]{2}"))
                    {
                        return FileListStyle.WindowsStyle;
                    }
                }
                return FileListStyle.Unknown;
            }
            //Если сервер работает на nix-ах
            private FileStruct ParseFileStructFromUnixStyleRecord(string record)
            {
                //Предположим. тчо запись имеет формат dr-xr-xr-x   1 owner    group    0 Nov 25  2002 bussys
                FileStruct f = new FileStruct();
                if (record[0] == '-' || record[0] == 'd')
                {// правильная запись файла
                    string processstr = record.Trim();
                    f.Flags = processstr.Substring(0, 9);
                    f.IsDirectory = (f.Flags[0] == 'd');
                    processstr = (processstr.Substring(11)).Trim();
                    //отсекаем часть строки
                    _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                    f.Owner = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                    f.CreateTime = getCreateTimeString(record);
                    Double.TryParse(processstr.Split(' ', '\t').Where(i => i != "").ToArray()[1], out f.Length);
                    //Индекс начала имени файла
                    int fileNameIndex = record.IndexOf(f.CreateTime) + f.CreateTime.Length;
                    //Само имя файла
                    f.Name = record.Substring(fileNameIndex).Trim();
                }
                else
                {
                    f.Name = "";
                }
                return f;
            }

            private string getCreateTimeString(string record)
            {
                //Получаем время
                string month = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)";
                string space = @"(\040)+";
                string day = "([0-9]|[1-3][0-9])";
                string year = "[1-2][0-9]{3}";
                string time = "[0-9]{1,2}:[0-9]{2}";
                Regex dateTimeRegex = new Regex(month + space + day + space + "(" + year + "|" + time + ")", RegexOptions.IgnoreCase);
                Match match = dateTimeRegex.Match(record);
                return match.Value;
            }

            private string _cutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
            {
                int pos1 = s.IndexOf(c, startIndex);
                string retString = s.Substring(0, pos1);
                s = (s.Substring(pos1)).Trim();
                return retString;
            }
        }

        public static FtpClient CreateClient()
        {
            FtpConfiguration ftpConfiguration = FtpConfiguration.Instance;
            string ftphost = ftpConfiguration.FtpSourcePath;
            ftphost = ftphost.Remove(0, "ftp://".Length);
            FtpClient ftpClient = new FtpClient(ftphost, ftpConfiguration.User, ftpConfiguration.Password);
            ftpClient.UseBinary = true;
            if (Configuration.Instance.UseProxy)
            {
                ftpClient.ProxyAddress = Configuration.Instance.ProxyAddress;
            }
            ftpClient.UseBinary = false;
            return ftpClient;
        }

        private string host;
        private string username;
        private string password;

        public FtpClient(string host, string username, string password)
        {
            this.host = host;
            this.username = username;
            this.password = password;
            useProxy = Configuration.Instance.UseProxy;
            proxyAddress = Configuration.Instance.ProxyAddress;
            usePassive = Configuration.Instance.UsePassiveFTP;
        }

        private const string TAG = "FTP Client";
        private bool usePassive = false;
        private int timeout = 0;
        private bool useProxy = false;
        private string proxyAddress = string.Empty;
        private DownloadingStatus _downloadingStatus;
        private CancellationTokenSource downloadingCancellationToken;
        private const int BUFFER_SIZE = 1024;
        private const int MULTIPLIER = 512;
        private long _bytesDownloaded;

        public bool UseSSL { get; set; } = false;
        public bool KeepAlive { get; set; } = true;
        public bool UseBinary { get; set; } = false;
        public bool UsePassive { get { return usePassive; } set { usePassive = value; } }
        public int Timeout { get { return timeout; } set { if (value > 0) timeout = value; } }
        public string ProxyAddress
        {
            get { return proxyAddress; }
            set
            {
                if (value != null)
                {
                    useProxy = true;
                    proxyAddress = value;
                }
            }
        }

        private FtpWebRequest CreateWebRequest(string path, string ftpMethod)
        {
            Uri uri = new Uri(string.Format("ftp://{0}{1}", host, path));
            FtpWebRequest webRequest = FtpWebRequest.Create(uri) as FtpWebRequest;
            webRequest.Credentials = new NetworkCredential(username, password);
            webRequest.KeepAlive = KeepAlive;
            webRequest.EnableSsl = UseSSL;
            webRequest.UseBinary = UseBinary;
            //webRequest.UsePassive = Configuration.Instance.UsePassiveFTP;
            if (timeout > 0) webRequest.Timeout = timeout;
            if (useProxy)
            {
                webRequest.Proxy = string.IsNullOrEmpty(proxyAddress) ? GlobalProxySelection.GetEmptyWebProxy() : new WebProxy(proxyAddress);
            }
            webRequest.Method = ftpMethod;
            return webRequest;
        }
        public FtpWebRequest CreateWebRequest(Uri uri, string method)
        {
            FtpWebRequest request = WebRequest.Create(uri) as FtpWebRequest;
            if (useProxy)
            {
                request.Proxy = string.IsNullOrEmpty(proxyAddress) ?
                    new WebProxy() : new WebProxy(proxyAddress);
            }
            request.UsePassive = Configuration.Instance.UsePassiveFTP;
            request.Credentials = new NetworkCredential(username, password);
            request.Method = method;
            request.KeepAlive = KeepAlive;
            return request;
        }

        private void DownloadFile(string sourceFile, string destinationFile)
        {
            _downloadingStatus = DownloadingStatus.InProgress;
            using (downloadingCancellationToken = new CancellationTokenSource())
            {
                FtpWebRequest request = CreateWebRequest(sourceFile, WebRequestMethods.Ftp.DownloadFile);
                FileInfo destinationFileInfo = new FileInfo(destinationFile);
                FileStream destinationFileStream = null;
                FtpWebResponse response = null;
                Stream responseStream = null;
                try
                {
                    if (destinationFileInfo.Exists)
                    {
                        request.ContentOffset = destinationFile.Length;
                        destinationFileStream = new FileStream(destinationFileInfo.FullName, FileMode.Append, FileAccess.Write);
                        _bytesDownloaded = destinationFile.Length;
                    }
                    else
                    {
                        destinationFileStream = new FileStream(destinationFileInfo.FullName, FileMode.Create, FileAccess.Write);
                        _bytesDownloaded = 0;
                    }
                    using (response = request.GetResponse() as FtpWebResponse)
                    {
                        using (responseStream = response.GetResponseStream())
                        {
                            int BUFFER = BUFFER_SIZE * MULTIPLIER;
                            byte[] buffer = new byte[BUFFER];
                            int bytesRead = responseStream.Read(buffer, 0, BUFFER);
                            while (bytesRead != 0 && !downloadingCancellationToken.IsCancellationRequested)
                            {
                                destinationFileStream.Write(buffer, 0, bytesRead);
                                _bytesDownloaded += bytesRead;
                                bytesRead = responseStream.Read(buffer, 0, BUFFER);
                            }
                        }
                    }
                }
                catch (WebException wEx)
                {
                    _downloadingStatus = DownloadingStatus.WebErrorOccured;
                    string errorStatus = ((FtpWebResponse)wEx.Response).StatusCode.ToString();
                    string errorStatusDescription = ((FtpWebResponse)wEx.Response).StatusDescription;
                }
                catch (Exception ex)
                {
                    _downloadingStatus = DownloadingStatus.ErrorOccurred;
                }
                finally
                {
                    if (response != null) response.Close();
                    if (responseStream != null) responseStream.Close();
                    if (destinationFileStream != null) destinationFileStream.Close();
                    destinationFileInfo.Refresh();
                    if (destinationFileInfo.Exists && destinationFileInfo.Length == 0) File.Delete(destinationFileInfo.FullName);
                }

                if (downloadingCancellationToken.IsCancellationRequested)
                {
                    // TODO: Cancellation Occurred, fire event
                    _downloadingStatus = DownloadingStatus.Stopped;
                }
                else
                {
                    // TODO: Downloading finished, fire event
                    _downloadingStatus = DownloadingStatus.Done;
                }
            }
            downloadingCancellationToken = null;
        }

        public FileStruct[] ListDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path[0] != '/') path = "/" + path;

            FtpWebRequest request = CreateWebRequest(path, WebRequestMethods.Ftp.ListDirectoryDetails);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                string content = string.Empty;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
                {
                    content = streamReader.ReadToEnd();
                    streamReader.Close();
                }
                DirectoryListParser parser = new DirectoryListParser(content);
                return parser.FullListing;
            }
        }

        public IEnumerable<FileStruct> FillCreateDateTime(string path, FileStruct[] filestructs)
        {
            for (int i = 0; i < filestructs.Length; i++)
            {
                DateTime dateTime = new DateTime();
                try
                {
                    FtpWebRequest request = CreateWebRequest(path + "/" + filestructs[i].Name, WebRequestMethods.Ftp.GetDateTimestamp);
                    using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                    {
                        string fileInfo = string.Empty;
                        dateTime = response.LastModified;
                        response.Close();
                    }
                }
                catch (WebException wEx)
                {
                    String status = ((FtpWebResponse)wEx.Response).StatusDescription;
                    Console.WriteLine(status);
                }
                filestructs[i].CreateDateTime = dateTime;
                yield return filestructs[i];
            }
        }

        public DateTime? GetLastModifiedFileDate(string pathUri)
        {
            Uri uri = new Uri(pathUri);
            FtpWebRequest request = CreateWebRequest(uri, WebRequestMethods.Ftp.GetDateTimestamp);
            try
            {
                using (FtpWebResponse response = request.GetResponse() as FtpWebResponse)
                {
                    return response.LastModified;
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("FtpClient GetFileDate {0} Error: {1}", pathUri, ex.Message);
                return null;
            }
        }

        public void SendReportToServer(FileInfo source, string destinationUrl)
        {
            try
            {
                if (source.Exists)
                {
                    Uri uri = new Uri(destinationUrl);
                    FtpWebRequest request = CreateWebRequest(uri, WebRequestMethods.Ftp.AppendFile);
                    request.UseBinary = false;
                    request.KeepAlive = true;
                    FileStream fs = File.OpenRead(source.FullName);
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Close();

                    Stream ftpstream = request.GetRequestStream();
                    ftpstream.Write(buffer, 0, buffer.Length);
                    ftpstream.Close();
                }
            }
            catch (WebException wEx)
            {
                string statusDescription = ((FtpWebResponse)wEx.Response).StatusDescription;
                Log.WriteError("Report - Status Description: {0}", statusDescription);
            }
            catch (Exception ex)
            {
                Log.WriteError("Report - error: {0}", ex.Message);
            }
        }

        public void SendReportToServer(Stream reportStream, Uri destinationUri)
        {
            try
            {
                FtpWebRequest request = CreateWebRequest(destinationUri, WebRequestMethods.Ftp.UploadFile);
                request.UseBinary = false;
                request.KeepAlive = true;
                using (Stream ftpStream = request.GetRequestStream())
                {
                    reportStream.CopyTo(ftpStream);
                    ftpStream.Close();
                }

            } catch (WebException wEx)
            {
                string statusDescription = ((FtpWebResponse)wEx.Response).StatusDescription;
                Log.WriteError("Report - Status Description: {0}", statusDescription);
            }
        }

        public long GetSourceFileSize(Uri sourceUri)
        {
            long sourceFileSize = 0;
            FtpWebRequest request = CreateWebRequest(sourceUri, WebRequestMethods.Ftp.GetFileSize);
            request.UseBinary = false;
            request.KeepAlive = true;
            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                sourceFileSize = response.ContentLength;
                Log.WriteTraceF(TAG, "GetSourceFileSize: {0} bytes", sourceFileSize);
                response.Close();
            }
            catch (WebException wEx)
            {
                String status = ((FtpWebResponse)wEx.Response).StatusDescription;
                Log.WriteTraceF(TAG, "GetSourceFileSize Error: {0}", status);
                if (sourceFileSize < 0) sourceFileSize = 0;
            }
            return sourceFileSize;
        }

        public Uri GetSourceUri(string sourcePath)
        {
            Uri uri = new Uri(string.Format("ftp://{0}{1}", host, sourcePath));
            return uri;
        }
    }
}
