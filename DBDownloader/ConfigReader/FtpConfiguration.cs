using Crypt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace DBDownloader.ConfigReader
{
    public class FtpConfiguration
    {
        private static readonly string CONFIG_FILE_STORAGE =
            string.Format(@"{0}", Directory.GetCurrentDirectory());
        private CryptRC4 rc4 = new CryptRC4(new Guid("61613f97-d29e-4df1-8254-15ec61187b3c").ToByteArray());
        private FtpConfigurationModel model;

        private static FtpConfiguration _instance = null;
        public static FtpConfiguration GetInstance()
        {
            if (_instance == null)
            {
                _instance = new FtpConfiguration();
            }
            return _instance;
        }
        public static FtpConfiguration Instance
        {
            get { return GetInstance(); }
        }

        private FtpConfiguration()
        {
            FileInfo configFile = new FileInfo(
                string.Format(@"{0}\{1}", CONFIG_FILE_STORAGE, Configuration.GetInstance().ConnectionInitFile));
            if (!configFile.Exists)
            {
                throw new Exception("Config file does not found.");
            }

            model = new FtpConfigurationModel();
            XmlSerializer serializer = new XmlSerializer(typeof(FtpConfigurationModel));
            StreamReader streamReader = null;
            try
            {
                byte[] encodeArray = File.ReadAllBytes(configFile.FullName);
                byte[] decodeArray = rc4.Decode(encodeArray, encodeArray.Length);
                string decodeString = ASCIIEncoding.UTF8.GetString(decodeArray);
                MemoryStream mStream = new MemoryStream();
                StreamWriter sw = new StreamWriter(mStream);
                sw.Write(decodeString);
                sw.Flush();
                mStream.Position = 0;

                model = serializer.Deserialize(mStream) as FtpConfigurationModel;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
        }

        public string FtpSourcePath { get { return model.FtpSourcePath.Trim(); } }
        public IList<ProductVersionModel> ProductModelItems { get { return model.ProductModelItems; } }
        public string AutocomplectsPath
        {
            get
            {
                return string.Format(@"{0}/{1}", model.FtpSourcePath, model.AutocomplectsPath);
            }
        }
        public string ProductsPath
        {
            get
            {
                return string.Format(@"{0}/{1}", model.FtpSourcePath, model.ProductsPath);
            }
        }
        public string DBPath { get { return model.DBPath; } }
        public string User { get { return model.User; } }
        public string Password { get { return model.Password; } }
        public string ReportsPath { get { return model.ReportsPath; } }
        public string ClearFolder { get { return model.ClearFolder; } }
        public HttpEndpointsModel HttpEndpoints { get { return model.HttpConfiguration.HttpEndpoints; } }
        public string HttpUser { get { return model.HttpConfiguration.User; } }
        public string HttpPassword { get { return model.HttpConfiguration.Password; } }
        public string SysInfoFtpPath { get { return model.SysInfoFtpPath; } }
        public string SysInfoAddrService { get { return model.SysInfoAddrService; } }
        public string SysInfoReportUrl { get { return model.SysInfoReportUrl; } }
    }
}
