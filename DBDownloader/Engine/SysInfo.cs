using DBDownloader.ConfigReader;
using DBDownloader.MainLogger;
using DBDownloader.Net.FTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DBDownloader.Engine
{
    public class SysInfo
    {
        private static string TAG = "SysInfo";
        private static Uri localSysInfoUri = new Uri(string.Format("{0}{1}", 
            FtpConfiguration.Instance.SysInfoAddrService, 
            FtpConfiguration.Instance.SysInfoReportUrl));
        public static void SendSysInfoToFtp()
        {
            if (string.IsNullOrEmpty(FtpConfiguration.Instance.SysInfoFtpPath)) return;

            string ftphost = FtpConfiguration.Instance.FtpSourcePath;
            ftphost = ftphost.Remove(0, "ftp://".Length);
            FtpClient ftpClient = new FtpClient(ftphost, FtpConfiguration.Instance.User, FtpConfiguration.Instance.Password);

            HttpWebResponse sysInfoResponse = null;
            Stream sysInfoStream = null;
            try
            {
                Log.WriteTraceF(TAG, "LocalSysInfoUrl: {0}", localSysInfoUri);
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(FtpConfiguration.Instance.SysInfoAddrService);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
                var getTask = httpClient.GetAsync(FtpConfiguration.Instance.SysInfoReportUrl);
                getTask.Wait();
                var responseMessage = getTask.Result;
                responseMessage.EnsureSuccessStatusCode();
                if (responseMessage.Content is StreamContent)
                {
                    var rm = (responseMessage.Content as StreamContent);
                    Uri reportDestinationUri = new Uri(FtpConfiguration.Instance.SysInfoFtpPath + "/" + rm.Headers.ContentDisposition.FileName.Trim('"'));
                    ftpClient.SendReportToServer(rm, reportDestinationUri);
                }
            }
            catch(WebException webEx)
            {
                Log.WriteError(string.Format("Web error has been occured, msg: {0}", webEx.Message));
                if (webEx.Response != null) {
                    HttpWebResponse response = webEx.Response as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.RedirectKeepVerb) ;
                }
            }
            catch(Exception ex)
            {
                Log.WriteError(string.Format("Error has been occured, msg: {0}", ex.Message));
            }
            finally
            {
                if (sysInfoResponse != null) sysInfoStream.Dispose();
                if (sysInfoStream != null) sysInfoStream.Dispose();
            }
        }
    }
}
