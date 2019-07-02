using DBDownloader.ConfigReader;
using DBDownloader.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBDownloader.Net.HTTP
{
    public class HttpClient : INetClient
    {
        public static HttpClient CreateClient()
        {
            HttpClient httpClient = new HttpClient();
            return httpClient;
        }

        public IEnumerable<FileStruct> FillCreateDateTime(string path, FileStruct[] filestructs)
        {
            throw new NotImplementedException();
        }

        public long GetSourceFileSize(Uri sourceUri)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use GetListKodup instead")]
        public FileStruct[] ListDirectory(string path)
        {
            return GetListKodup();
        }

        private HttpWebRequest CreateWebRequest(Uri uri, string method)
        {
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;

            if (Configuration.Instance.UseProxy)
            {
                request.Proxy = string.IsNullOrEmpty(Configuration.Instance.ProxyAddress) ?
                    new WebProxy() : new WebProxy(Configuration.Instance.ProxyAddress);
            }
            request.Credentials = UserService.Instance.GetNetworkCredential();
            request.Method = method;
            return request;
        }

        private readonly string http_user = FtpConfiguration.Instance.HttpUser; // "kodup";
        private readonly string http_password = FtpConfiguration.Instance.HttpPassword; // "update";

        private readonly bool proxy_use = false;
        private readonly string proxy_address = "";

        private readonly string db_list = string.Format("/{0}", 
            FtpConfiguration.Instance.HttpEndpoints.DBList);
        private readonly string login_endpoint = string.Format("/{0}", 
            FtpConfiguration.Instance.HttpEndpoints.Login);

        private readonly string server_ip = FtpConfiguration.Instance.HttpEndpoints.BaseIp; //"82.208.93.53";

        private readonly int BYTE_BUFFER_SIZE = 1024 * 512; // 1/2 mb
        private long bytesDownloaded;

        private CancellationTokenSource loopCancellationTokenSource = null;

        public int RepeatCount { get; set; } = 10;
        public int DelayTime { get; set; } = 10000;

        private static Cookie authCookie = null;
        private List<Cookie> add_cookies = new List<Cookie>();
        private KodupPageParser kodupPageParser = new KodupPageParser();

        private HttpWebRequest CreateHttpRequest(Uri uri, string method)
        {
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            if (proxy_use)
            {
                request.Proxy = string.IsNullOrEmpty(proxy_address) ?
                    new WebProxy() : new WebProxy(proxy_address);
            }
            request.Method = method;
            request.Accept = "text/html,application/xhtml+xml,application/xml";
            request.UserAgent = "KodUp";
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Host = server_ip;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 2;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            if (authCookie != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(uri, authCookie);
            }

            if (proxy_use)
            {
                request.Proxy = string.IsNullOrEmpty(proxy_address) ?
                    new WebProxy() : new WebProxy(proxy_address);

            }

            return request;
        }

        private bool DownloadFile(Uri sourceFile, FileInfo destinationFileInfo)
        {

            HttpWebRequest request = CreateHttpRequest(sourceFile, WebRequestMethods.Http.Get);
            FileStream fileStream = null;
            try
            {
                if (destinationFileInfo.Exists)
                {
                    request.AddRange(destinationFileInfo.Length);
                    fileStream = destinationFileInfo.OpenWrite();
                    fileStream.Seek(fileStream.Length, SeekOrigin.Current);
                    bytesDownloaded = destinationFileInfo.Length;
                }
                else
                {
                    fileStream = new FileStream(destinationFileInfo.FullName, FileMode.Create);
                    bytesDownloaded = 0;
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream responseStream = response.GetResponseStream();
                    byte[] buffer = new byte[BYTE_BUFFER_SIZE];
                    int readByteSize = responseStream.Read(buffer, 0, BYTE_BUFFER_SIZE);
                    while (readByteSize != 0)
                    {
                        fileStream.Write(buffer, 0, readByteSize);
                        bytesDownloaded += readByteSize;
                        readByteSize = responseStream.Read(buffer, 0, BYTE_BUFFER_SIZE);
                    }
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
            return true;
        }

        public bool DownloadFile(Uri sourceFile, FileInfo destinationFileInfo, CancellationTokenSource cancellationToken)
        {
            HttpWebRequest request = CreateHttpRequest(sourceFile, WebRequestMethods.Http.Get);
            FileStream fileStream = null;
            try
            {
                if (destinationFileInfo.Exists)
                {
                    request.AddRange(destinationFileInfo.Length);
                    fileStream = destinationFileInfo.OpenWrite();
                    fileStream.Seek(fileStream.Length, SeekOrigin.Current);
                    bytesDownloaded = destinationFileInfo.Length;
                }
                else
                {
                    fileStream = new FileStream(destinationFileInfo.FullName, FileMode.Create);
                    bytesDownloaded = 0;
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream responseStream = response.GetResponseStream();
                    byte[] buffer = new byte[BYTE_BUFFER_SIZE];
                    int readByteSize = responseStream.Read(buffer, 0, BYTE_BUFFER_SIZE);
                    while (readByteSize != 0 && 
                        (cancellationToken != null ? !cancellationToken.IsCancellationRequested : false))
                    {
                        fileStream.Write(buffer, 0, readByteSize);
                        bytesDownloaded += readByteSize;
                        readByteSize = responseStream.Read(buffer, 0, BYTE_BUFFER_SIZE);
                    }
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
            return true;
        }

        private void CheckAndUpdateAuth(string ret_path) {
            if (authCookie == null || authCookie.Expired)
            {
                GetAuthCookie(ret_path);
            }
        }

        public Task DownloadFileAsync(Uri sourceFile, FileInfo destinationFileInfo)
        {
            CheckAndUpdateAuth(sourceFile.LocalPath);
            return Task.Factory.StartNew(() =>
            {
                int loopCount = RepeatCount;
                do
                {
                    try
                    {
                        if (DownloadFile(sourceFile, destinationFileInfo))
                            break;
                    }
                    catch (WebException wEx)
                    {
                        using (loopCancellationTokenSource = new CancellationTokenSource())
                        {
                            loopCancellationTokenSource.Token.WaitHandle.WaitOne(DelayTime);
                        }
                        loopCancellationTokenSource = null;
                        if (wEx.Status == WebExceptionStatus.RequestCanceled)
                        {
                            loopCount = 0;
                        }
                        else
                        {
                            loopCount--;
                        }
                    }

                } while (loopCount > 0);
            });
        }

        public FileStruct[] GetListKodup()
        {

            CheckAndUpdateAuth(db_list);

            List<FileStruct> fileStructs = new List<FileStruct>();

            Uri uri = new Uri(string.Format("http://{0}{1}", server_ip, db_list));
            var request = CreateHttpRequest(uri, WebRequestMethods.Http.Get);

            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (response.CharacterSet == null)
                    {
                        readStream = new StreamReader(receiveStream);
                    }
                    else
                    {
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    }
                    kodupPageParser.Load(readStream);
                    readStream.Close();
                }
            }
            catch (WebException wEx)
            {
                Console.WriteLine(wEx.Message);
                HttpWebResponse response = (HttpWebResponse)wEx.Response;
                if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("Forbidden! Wrong user/password pair.");
                }
                response.Close();
            }
            if (kodupPageParser != null && kodupPageParser.GetDatabaseFileList() != null && kodupPageParser.GetDatabaseFileList().Length > 0)
            {
                foreach(var item in kodupPageParser.GetDatabaseFileList())
                {
                    fileStructs.Add(new FileStruct()
                    {
                        Name = item.Name,
                        Length = item.Size,
                        IsDirectory = false,
                        LastModifiedDateTime = item.LastModified
                    });
                }
            }
            return fileStructs.ToArray();
        }

        private void GetAuthCookie(string path_endpoint)
        {
            string postData = string.Format("user={0}&pass={1}&path={2}",
                http_user, http_password, path_endpoint);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            Uri uri = new Uri(string.Format("http://{0}{1}", server_ip, login_endpoint));
            HttpWebRequest request = CreateHttpRequest(uri, WebRequestMethods.Http.Post);
            request.Accept = "application/json, text/javascript, */*";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            string auth = string.Empty;
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Console.WriteLine("Status description: {0}", response.StatusDescription);
                    using (dataStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(dataStream);
                        auth = reader.ReadToEnd();
                        Console.WriteLine(auth);
                        var set_cookie = response.Headers["Set-Cookie"];
                        if (set_cookie != null)
                        {
                            foreach (var item in set_cookie.Split(';'))
                            {
                                string[] item_values = item.Trim().Split('=');
                                if (item_values[0].Equals("Auth"))
                                {
                                    authCookie = new Cookie("Auth", item_values[1]); //{ Domain = request.Host }
                                }
                                else
                                {
                                    add_cookies.Add(new Cookie(item_values[0], item_values[1]) { Domain = request.Host });
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException wEx)
            {
                Console.WriteLine(wEx.Message);
                authCookie = null;
            }
        }

        public HttpWebResponse GetHttpWebResponse(Uri uri, string httpMethod, bool isAuthNeeded = false)
        {
            if (uri != null)
            {
                if (isAuthNeeded) CheckAndUpdateAuth(uri.AbsolutePath);
                var request = CreateHttpRequest(uri, httpMethod);
                var response = request.GetResponse();
                return response as HttpWebResponse;
            }
            throw new ArgumentNullException("Uri can't be null");
        }

        public Uri GetSourceUri(string sourcePath)
        {
            Uri uri = new Uri(string.Format("http://{0}/{1}", server_ip, sourcePath));
            return uri;
        }
    }
}
