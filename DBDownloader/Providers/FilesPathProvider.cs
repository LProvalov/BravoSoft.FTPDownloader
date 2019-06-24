using DBDownloader.ConfigReader;
using DBDownloader.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Providers
{
    public class FilesPathProvider
    {
        private static readonly string AUTOCOMPLECTS_FILENAME_TEMPLATE = "AutoComplects_{0}.xml";
        private static RegFileSearch _regFileSearch = null;
        public static RegFileSearch RegFileSearch
        {
            get
            {
                if (_regFileSearch == null)
                    _regFileSearch = new RegFileSearch(Configuration.Instance.RegFileInfo);
                return _regFileSearch;
            }
        }

        public static string AutocomplectsFileName
        {
            get
            {
                return string.Format(AUTOCOMPLECTS_FILENAME_TEMPLATE, RegFileSearch.DistributorCode);
            }
        }
        public static string GetAutoComplectFileInfo()
        {
            if (Configuration.Instance.NetClientType == Net.NetFileDownloader.NetClientTypes.FTP)
            {
                return string.Format(@"{0}/{1}",
                    FtpConfiguration.Instance.AutocomplectsPath, AutocomplectsFileName);
            }
            if (Configuration.Instance.NetClientType == Net.NetFileDownloader.NetClientTypes.HTTP)
            {
                return string.Format(@"{0}/{1}",
                    FtpConfiguration.Instance.HttpEndpoints.Xml, AutocomplectsFileName);
            }
            throw new ArgumentException("Wrong net client type");
        }
        public static string GetProductFilesDirPath()
        {
            if (Configuration.Instance.NetClientType == Net.NetFileDownloader.NetClientTypes.FTP)
            {
                return FtpConfiguration.Instance.ProductsPath;
            }
            if (Configuration.Instance.NetClientType == Net.NetFileDownloader.NetClientTypes.HTTP)
            {
                return string.Format(@"http://{0}/{1}",
                    FtpConfiguration.Instance.HttpEndpoints.BaseIp,
                    FtpConfiguration.Instance.HttpEndpoints.Xml);
            }
            throw new ArgumentException("Wrong net client type");
        }

        public static string GetDBPath()
        {
            if (Configuration.Instance.NetClientType == Net.NetFileDownloader.NetClientTypes.FTP)
            {
                return string.Format(@"{0}//{1}",
                    FtpConfiguration.Instance.FtpSourcePath, FtpConfiguration.Instance.DBPath);
            }
            if (Configuration.Instance.NetClientType == Net.NetFileDownloader.NetClientTypes.HTTP)
            {
                return string.Format(@"http://{0}/{1}",
                    FtpConfiguration.Instance.HttpEndpoints.BaseIp,
                    FtpConfiguration.Instance.HttpEndpoints.DBList);
            }
            throw new ArgumentException("Wrong net client type");
        }
    }
}
