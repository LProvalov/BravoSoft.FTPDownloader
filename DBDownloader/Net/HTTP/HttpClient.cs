using DBDownloader.ConfigReader;
using DBDownloader.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        private readonly string ftp_user = "user1";
        private readonly string ftp_password = "1q2w3e";

        private readonly string http_user = "kodup";
        private readonly string http_password = "update";

        private readonly bool proxy_use = false;
        private readonly string proxy_address = "";

        private readonly string kodup_endpoint = "/kodup";
        private readonly string login_endpoint = "/users/login.asp";

        private readonly string server_ip = "82.208.93.53";

        private Cookie authCookie = null;
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
            request.AutomaticDecompression = DecompressionMethods.GZip;
            if (authCookie != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(authCookie);
            }

            if (proxy_use)
            {
                request.Proxy = string.IsNullOrEmpty(proxy_address) ?
                    new WebProxy() : new WebProxy(proxy_address);

            }

            return request;
        }

        public FileStruct[] GetListKodup()
        {
            FileStruct[] fileStructs = null;

            Uri uri = new Uri(string.Format("http://{0}{1}", server_ip, kodup_endpoint));
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
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("Forbidden! Wrong user/password pair.");
                }
                response.Close();
            }

            return fileStructs;
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
                                    authCookie = new Cookie("Auth", item_values[1]) { Domain = request.Host };
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

    }
}
