using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using Crypt;

namespace DBDownloader.ConfigReader
{
    public class FtpConfiguration
    {
        private CryptRC4 rc4 = new CryptRC4(new Guid("61613f97-d29e-4df1-8254-15ec61187b3c").ToByteArray());

        private FtpConfigurationModel model;

        public FtpConfiguration(string configurationPath)
        {
            model = new FtpConfigurationModel();
            XmlSerializer serializer = new XmlSerializer(typeof(FtpConfigurationModel));
            StreamReader streamReader = null;
            try
            {
                byte[] encodeArray = File.ReadAllBytes(configurationPath);
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
                if (streamReader != null) streamReader.Close();
            }
        }

        public string FtpSourcePath { get { return model.FtpSourcePath; } }
        public IList<string> ProductFilesPath { get { return model.ProductFileNames; } }
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
    }
}
