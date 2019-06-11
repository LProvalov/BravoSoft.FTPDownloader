using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DBDownloader.ConfigReader
{
    public class Configuration
    {
        private ConfigurationModel model;
        private XmlSerializer serializer;
        private string configurationPath;

        private bool isLoaded;

        public Configuration(string configurationPath)
        {
            model = new ConfigurationModel();
            serializer = new XmlSerializer(typeof(ConfigurationModel));
            isLoaded = false;
            this.configurationPath = configurationPath;
        }

        public void LoadConfiguration()
        {
            DeserializeConfigModel(configurationPath);
            isLoaded = true;
        }

        public void SaveConfiguration()
        {
            SerializeConfigModel(configurationPath);
        }

        private void SerializeConfigModel(string configPath)
        {
            StreamWriter streamWriter = null;
            try
            {
                streamWriter = new StreamWriter(configPath, false);
                serializer.Serialize(streamWriter, model);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (streamWriter != null) streamWriter.Close();
            }
        }

        private bool DeserializeConfigModel(string configPath)
        {
            FileInfo configFile = new FileInfo(configPath);
            if (!configFile.Exists) return false;
            StreamReader streamReader = null;
            try
            {
                streamReader = new StreamReader(configPath);
                model = serializer.Deserialize(streamReader) as ConfigurationModel;
            }
            catch (InvalidOperationException ioEx)
            {

            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (streamReader != null) streamReader.Close();
            }
            return true;
        }

        public FileInfo RegFileInfo
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                if (string.IsNullOrEmpty(model.RegFile)) return null;
                return new FileInfo(model.RegFile);
            }
            set
            {
                model.RegFile = value.FullName;
            }
        }

        public DirectoryInfo OperationalUpdateDirectory
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                if (string.IsNullOrEmpty(model.OperationalUpdateDirectory)) return null;
                return new DirectoryInfo(model.OperationalUpdateDirectory);
            }
            set
            {
                model.OperationalUpdateDirectory = value.FullName;
            }
        }

        public DirectoryInfo DBDirectory
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                if (string.IsNullOrEmpty(model.DBDirectory)) return null;
                return new DirectoryInfo(model.DBDirectory);
            }
            set
            {
                model.DBDirectory = value.FullName;
            }
        }

        public bool IsTechnicalRegulationReform
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.IsTechnicalRegulationReform;
            }
            set { model.IsTechnicalRegulationReform = value; }
        }

        public string ConnectionInitFile
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.ConnectionInitFile;
            }
        }

        public bool UseProxy
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.UseProxy;
            }
        }

        public string ProxyAddress
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.ProxyAddress;
            }
        }

        public DateTime DelayedStart
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.DelayedStart;
            }
            set
            {
                model.DelayedStart = value;
            }
        }

        public IEnumerable<string> KTServices
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                foreach (string str in model.KTServices.Split(','))
                {
                    yield return str;
                }
            }
        }

       /* 
        public int ProductVersion
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.ProductVersion;
            }
            set
            {
                model.ProductVersion = value;
            }
        }
        */

        public int CountOfRepeat
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.CountOfRepeat;
            }
        }

        public int RepeatDalay
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.RepeatDalay;
            }
        }

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(model.RegFile) || string.IsNullOrEmpty(model.OperationalUpdateDirectory)
                    || string.IsNullOrEmpty(model.DBDirectory))
                    return false;
                return new FileInfo(model.RegFile).Exists &&
                    new DirectoryInfo(model.OperationalUpdateDirectory).Exists &&
                    new DirectoryInfo(model.DBDirectory).Exists;
            }
        }

        public bool UsePassiveFTP
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading.");
                return model.UsePassiveFTP;
            }
        }

        public bool AutoStart
        {
            get
            {
                if (!isLoaded) throw new Exception("Configuration does not loading");
                return model.AutoStart;
            }
        }
    }
}
