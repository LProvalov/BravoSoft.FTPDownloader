using System;

namespace DBDownloader.ConfigReader
{
    [Serializable]
    public class ConfigurationModel
    {
        public string RegFile { get; set; }
        public string OperationalUpdateDirectory { get; set; }
        public string DBDirectory { get; set; }
        public bool IsTechnicalRegulationReform { get; set; }
        public string ConnectionInitFile { get; set; }
        public bool UseProxy { get; set; }
        public string ProxyAddress { get; set; }
        public bool UsePassiveFTP { get; set; }
        public DateTime DelayedStart { get; set; }
        public string KTServices { get; set; }
        public int ProductVersion { get; set; }
        public bool AutoStart { get; set; }
        public int CountOfRepeat { get; set; }
        public int RepeatDalay { get; set; }
    }
}

