using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DBDownloader
{
    public static class ReportWriter
    {
        private static object syncObject = new object();
        private static FileInfo reportInfo = null;

        public static void SetReportInfo(FileInfo reportInfo)
        {
            ReportWriter.reportInfo = reportInfo;
        }

        public static void AppendString(string str)
        {
            lock (syncObject)
            {
                File.AppendAllText(reportInfo.FullName, str);
            }
        }

        public static void AppendString(string formatString, params object[] args)
        {
            string str = string.Format(formatString + "\n", args);
            AppendString(str);
        }

        public static FileInfo GetReportInfo() {
            return reportInfo;
        }
    }
}
