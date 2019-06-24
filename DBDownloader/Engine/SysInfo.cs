using DBDownloader.ConfigReader;
using DBDownloader.MainLogger;
using DBDownloader.Net.FTP;
using DBDownloader.Net.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace DBDownloader.Engine
{
    public class SysInfo
    {
        private static string TAG = "SysInfo";
        private static HttpClient httpClient = new HttpClient();
        private static Uri localSysInfoUri = new Uri(string.Format("{0}{1}", 
            FtpConfiguration.Instance.SysInfoAddrService, 
            FtpConfiguration.Instance.SysInfoReportUrl));
        public static void SendSysInfoToFtp()
        {
            if (string.IsNullOrEmpty(FtpConfiguration.Instance.SysInfoFtpPath)) return;

            string ftphost = FtpConfiguration.Instance.FtpSourcePath;
            ftphost = ftphost.Remove(0, "ftp://".Length);
            FtpClient ftpClient = new FtpClient(ftphost, FtpConfiguration.Instance.User, FtpConfiguration.Instance.Password);

            HttpWebResponse sysInfoRequest = null;
            Stream sysInfoStream = null;
            try
            {
                Log.WriteTraceF(TAG, "LocalSysInfoUrl: {0}", localSysInfoUri);
                sysInfoRequest = httpClient.GetHttpWebResponse(localSysInfoUri, WebRequestMethods.Http.Get);
                sysInfoStream = sysInfoRequest.GetResponseStream();
                Uri reportDestinationUri = new Uri(FtpConfiguration.Instance.SysInfoFtpPath);
                ftpClient.SendReportToServer(sysInfoStream, reportDestinationUri);
                sysInfoStream.Close();
            } finally
            {
                if (sysInfoRequest != null) sysInfoStream.Dispose();
                if (sysInfoStream != null) sysInfoStream.Dispose();
            }
        }
    }
}
