using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DBDownloader.ConfigReader;
using DBDownloader.Models;
using DBDownloader.XML;
using DBDownloader.XML.Models.Autocomplects;
using DBDownloader.XML.Models.Products;
using DBDownloader.FTP;
using DBDownloader.MainLogger;
using System.Text;

namespace DBDownloader
{
    public enum Status
    {
        InProgress,
        Stopping,
        Stopped,
        StartWaiting
    }
    public class DownloaderManager
    {
        private Status _status;
        public Status Status
        {
            get
            {
                return this._status;
            }
            set
            {
                if (StatusChangeEvent != null)
                    StatusChangeEvent.BeginInvoke(this, new StatusEntryArgs(value), null, null);
                this._status = value;
            }
        }
        private ObservableCollection<DownloadingItem> downloadingItems;
        public ObservableCollection<DownloadingItem> DownloadingItems { get { return downloadingItems; } }

        public ObservableCollection<DownloadingItem> NNTD_DownloadingItems { get; set; }

        public event EventHandler InitializeEnd;
        public event EventHandler<StatusEntryArgs> StatusChangeEvent;
        private Task processingTask;

        // TODO: Replace event classes to another place.
        public class StringEntryEventArgs : EventArgs
        {
            private readonly string str;
            public StringEntryEventArgs(string str)
            {
                this.str = str;
            }
            public string Text { get { return str; } }
        }
        public class StatusEntryArgs : EventArgs
        {
            private readonly Status _status;
            public StatusEntryArgs(Status status)
            {
                this._status = status;
            }
            public Status Status { get { return _status; } }
        }

        public event EventHandler ProcessingEndedEvent;
        public event EventHandler<StringEntryEventArgs> SendConsoleMessage;

        private Configuration configuration;
        private FtpConfiguration ftpConfiguration;
        private SchedulerModel scheduler;
        private RegFileSearch regFileSearch;

        private LoadingManager loadingManager;

        private NetworkCredential networkCredential;
        private FTPWorker ftpWorker;
        private bool useProxy = false;
        private string proxyAddress = string.Empty;
        private CancellationTokenSource waitingStartCts;
        private bool isStopServicesNeeded = false;

        private string reportUri;
        private readonly string REPORT_PATH = "report.txt";

        private readonly string AUTOCOMPLECTS_FILENAME_TEMPLATE = "AutoComplects_{0}.xml";
        private readonly string CONFIG_FILE_STORAGE =
            string.Format(@"{0}", Directory.GetCurrentDirectory());

        public DownloaderManager(Configuration configuration, SchedulerModel scheduler,
            bool useProxy = false, string proxyAddress = "")
        {
            this.configuration = configuration;
            this.scheduler = scheduler;
            this.Status = Status.Stopped;
            this.useProxy = useProxy;
            this.proxyAddress = proxyAddress;
            downloadingItems = new ObservableCollection<DownloadingItem>();
            NNTD_DownloadingItems = new ObservableCollection<DownloadingItem>();
            FileInfo reportinfo = new FileInfo(REPORT_PATH);
            if (reportinfo.Exists) reportinfo.Delete();
            ReportWriter.SetReportInfo(reportinfo);
        }

        public DateTime DelayedStart { get; set; }

        private IList<Tom> GetListOfToms()
        {
            Log.WriteInfo("DownloaderManager GetListOfToms");
            FtpFilesProvider ftpFilesProvider =
                useProxy ?
                new FtpFilesProvider(networkCredential, useProxy, configuration.UsePassiveFTP, proxyAddress) :
                new FtpFilesProvider(networkCredential, usePassiveFTP: configuration.UsePassiveFTP);

            string autocomplectsFileName =
                string.Format(AUTOCOMPLECTS_FILENAME_TEMPLATE, regFileSearch.DistributorCode);

            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("autocomplect file downloading..."));
            FileInfo autoComplectFileInfo = ftpFilesProvider.GetFile(
                string.Format(@"{0}\{1}", CONFIG_FILE_STORAGE, autocomplectsFileName),
                string.Format(@"{0}/{1}", ftpConfiguration.AutocomplectsPath, autocomplectsFileName));

            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Product files downloading..."));
            IEnumerable<FileInfo> productFiles =
                ftpFilesProvider.GetProductsFiles(CONFIG_FILE_STORAGE, ftpConfiguration);

            FileInfo[] autoComplectsFiles = new FileInfo[] { autoComplectFileInfo };
            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Autocomplect file parsing..."));
            AutoComplectsParser autoComplectsParser =
                new AutoComplectsParser(autoComplectsFiles, regFileSearch.ClientCode);
            IList<AutoComplect> productIds = autoComplectsParser.GetProductKeys();
            productIds.Add(new AutoComplect() { Ref = regFileSearch.ClientCode, Kompl = 12000 });
            if (configuration.IsTechnicalRegulationReform)
            {
                productIds.Add(new AutoComplect() { Ref = regFileSearch.ClientCode, Kompl = 0 });
            }

            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Product files parsing..."));
            ProductsParser productParser = new ProductsParser(productFiles);
            string productListName = ftpConfiguration.ProductFilesPath[configuration.ProductVersion];
            return productParser.GetDBList(productIds, productListName);
        }

        private void Initialize()
        {
            Log.WriteInfo("DownloaderManager Initialize");
            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Initializating..."));

            Log.WriteTrace("Initialize - config file");
            FileInfo configFile = new FileInfo(
                    string.Format(@"{0}\{1}", CONFIG_FILE_STORAGE, configuration.ConnectionInitFile));
            if (!configFile.Exists) throw new Exception("Config file does not found.");

            Log.WriteTrace("Initialize - configuration reader");

            ftpConfiguration = new FtpConfiguration(configFile.FullName);

            Log.WriteTrace("Initialize - reg file search");
            regFileSearch = new RegFileSearch(configuration.RegFileInfo);

            networkCredential =
               new NetworkCredential(ftpConfiguration.User, ftpConfiguration.Password);

            reportUri = string.Format(@"{0}//{1}/{2}",
                ftpConfiguration.FtpSourcePath, ftpConfiguration.ReportsPath, regFileSearch.ClientCode);
            string reportsDir = reportUri + ".txt";
            ReportWriter.AppendString("Клиент ID - {0}. {1}.\n", regFileSearch.ClientCode, DateTime.Now.ToLongTimeString());

            Log.WriteTrace("Initialize - loading manager");
            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Loading manager initialization..."));
            loadingManager = new LoadingManager(networkCredential, new Uri(reportsDir), configuration);
            if (useProxy)
            {
                loadingManager.UseProxy = useProxy;
                loadingManager.ProxyAddress = proxyAddress;
            }
            loadingManager.ErrorOccurred += LoadingManager_ErrorOccurred;
            Log.WriteTrace("Initialize - ftp worker");
            ftpWorker = new FTPWorker(networkCredential, useProxy, proxyAddress, configuration.UsePassiveFTP);
            string dbUriStr = string.Format(@"{0}//{1}",
                ftpConfiguration.FtpSourcePath, ftpConfiguration.DBPath);
            Dictionary<string, FtpFileInfo> sizeDBDictionary =
                ftpWorker.GetDBListWithSize(new Uri(dbUriStr));
            if (sizeDBDictionary.Count == 0) throw new Exception(
                string.Format("Can't get information about db files in current Url: {0}", dbUriStr));

            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Getting info about FTP DB files..."));
            ftpWorker.GetFilesDate(dbUriStr);

            Log.WriteTrace("Initialize - get list of toms");
            SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Forming downloading queue..."));
            List<string> missingFtpFiles = new List<string>();
            List<string> downloadedFtpFiles = new List<string>();

            foreach (Tom tom in GetListOfToms())
            {
                string destinationFileStr = string.Format(@"{0}\{1}",
                    configuration.DBDirectory.FullName,
                    tom.FileName);
                string sourceFileUrl = string.Format(@"{0}//{1}/{2}",
                    ftpConfiguration.FtpSourcePath,
                    ftpConfiguration.DBPath, tom.FileName);
                FtpFileInfo sourceFileInfo;

                long sourceSize = 0;
                bool isUpdateNeeded = false;
                DateTime creationFileDateTime = new DateTime();
                FileInfo destinationFile = new FileInfo(destinationFileStr);
                if (sizeDBDictionary.TryGetValue(tom.FileName, out sourceFileInfo))
                {
                    sourceSize = sourceFileInfo.Length;
                    creationFileDateTime = sourceFileInfo.LastModified;
                    if (!destinationFile.Exists) isUpdateNeeded = true;
                    else if (destinationFile.CreationTime.Date != sourceFileInfo.LastModified.Date)
                    {
                        Log.WriteTrace("{0} destDate :{1} != sourceDate :{2}",
                            tom.FileName, destinationFile.CreationTime.Date, sourceFileInfo.LastModified.Date);
                        isUpdateNeeded = true;
                    }
                }
                else
                {
                    Log.WriteTrace("{0} no file on FTP", tom.FileName);
                    missingFtpFiles.Add(tom.FileName);
                    isUpdateNeeded = false;
                }
                if (isUpdateNeeded) downloadedFtpFiles.Add(tom.FileName);
                Log.WriteTrace("{0} isUpdateNeeded:{1}", tom.FileName, isUpdateNeeded);
                loadingManager.AddFileToDownload(tom.Name, tom.FileName,
                    destinationFile, sourceFileUrl, creationFileDateTime,
                    isUpdateNeeded, sourceSize);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Построение очереди загрузки - ОК");
            sb.AppendLine("Пропущены файлы отсутствующие на сервере:");
            foreach (string str in missingFtpFiles) sb.AppendLine(str);
            sb.AppendLine("Очередь загрузки:");
            foreach (string str in downloadedFtpFiles) sb.AppendLine(str);
            ReportWriter.AppendString(sb.ToString());

            if (InitializeEnd != null)
                InitializeEnd.Invoke(this, new EventArgs());
        }

        private void LoadingManager_ErrorOccurred(object sender, ErrorEventArgs e)
        {
            Exception ex = e.GetException();
            string message = ex.Message;
            if (ex.InnerException != null)
                message += string.Format(" InnerException:{0}", ex.InnerException.Message);
            SendConsoleMessage.Invoke(this, new StringEntryEventArgs(message));
        }

        // TODO: Can realize service class what will be implement all method for working with it.
        private void StopKTServices()
        {
            Log.WriteInfo("stop services");
            foreach (string serviceName in configuration.KTServices)
            {
                if (ServiceWorker.ServiceExists(serviceName) &&
                    ServiceWorker.GetServiceStatus(serviceName) ==
                        System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    InputMessage(string.Format("{0} trying to pause", serviceName));
                    if (ServiceWorker.PauseService(serviceName))
                    {
                        InputMessage(string.Format("{0} service paused", serviceName));
                        ReportWriter.AppendString("Остановка службы {0} - ОК\n", serviceName);
                        isStopServicesNeeded = true;
                    }
                    else
                    {
                        SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Can't pause service, see logs for more information."));
                        ReportWriter.AppendString("Остановка службы {0} - FAILED\n", serviceName);
                    }
                }
                else
                {
                    InputMessage(string.Format("{0} service does not found or not running", serviceName));
                }
            }
        }
        private void StartKTServices()
        {
            Log.WriteInfo("start services, isStopServicesNeeded:{0}", isStopServicesNeeded);
            if (isStopServicesNeeded)
            {
                isStopServicesNeeded = false;
                foreach (string serviceName in configuration.KTServices)
                {
                    if (ServiceWorker.ServiceExists(serviceName))
                    {
                        var serviceStatus = ServiceWorker.GetServiceStatus(serviceName);
                        if (serviceStatus == System.ServiceProcess.ServiceControllerStatus.Stopped ||
                            serviceStatus == System.ServiceProcess.ServiceControllerStatus.Paused)
                        {
                            InputMessage(string.Format("{0} trying to start", serviceName));
                            if (ServiceWorker.ContinueService(serviceName))
                            {
                                InputMessage(string.Format("{0} service started", serviceName));
                                ReportWriter.AppendString("Запуск службы {0} - ОК\n", serviceName);
                            }
                            else
                            {
                                SendConsoleMessage.Invoke(this, new StringEntryEventArgs("Can't start service, see logs for more information."));
                                ReportWriter.AppendString("Запуск службы {0} - FAILED\n", serviceName);
                            }
                        }
                        else
                        {
                            InputMessage(string.Format("{0} service does not found or not stopped", serviceName));
                        }
                    }
                }
            }
        }
        private bool DelayedStartFunc()
        {
            if (DelayedStart > DateTime.Now)
            {
                Status = DBDownloader.Status.StartWaiting;
                TimeSpan waitingTime = DelayedStart - DateTime.Now;
                using (waitingStartCts = new CancellationTokenSource())
                {
                    try
                    {
                        string message = string.Format("Delayed start {0}, waitingtime: {1}, Download will begin in: {2}",
                            DateTime.Now.ToLongTimeString(), waitingTime.ToString(), DateTime.Now.Add(waitingTime));
                        InputMessage(message);
                        ReportWriter.AppendString(message);

                        waitingStartCts.Token.WaitHandle.WaitOne(waitingTime);
                        Log.WriteTrace("Stop waiting {0}", DateTime.Now.ToLongTimeString());
                    }
                    catch (Exception ex)
                    {
                        Log.WriteError("Processing Error: {0}", ex.Message);
                    }
                    long dt = DelayedStart.Ticks;
                    long nt = DateTime.Now.Ticks;
                    if (dt > nt)
                    {
                        Log.WriteTrace("DelayedStart ticks:{0}, now ticks:{1}", dt, nt);
                        Status = DBDownloader.Status.Stopped;
                        if (ProcessingEndedEvent != null)
                            ProcessingEndedEvent.Invoke(this, new EventArgs());
                        return false;
                    }                    
                }
            }
            return true;
        }
        private void Processing()
        {
            Log.WriteInfo("DownloadingManager Processing");
            if (DelayedStartFunc())
            {
                try
                {
                    Initialize();
                    Status = DBDownloader.Status.InProgress;
                    if (loadingManager.DownloadingFileLenght > 0)
                    {
                        StopKTServices();
                        loadingManager.BeginAsync().Wait();
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = string.Format("Processing Error: Initialize error: {0}", ex.Message);
                    Log.WriteError(errorMessage);
                    SendConsoleMessage.Invoke(this, new StringEntryEventArgs(errorMessage));
                }

                if (ProcessingEndedEvent != null)
                    ProcessingEndedEvent.Invoke(this, new EventArgs());

                if (loadingManager.IsLoadedEnd)
                {
                    ReportWriter.AppendString("Обновление завершено. {0} {1}\n", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    CleanOperUp();
                }
                else
                {
                    ReportWriter.AppendString("Загрузка приостановлена. {0} {1}\n", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                }
                StartKTServices();
                string reportDate = string.Format("d{0}{1}{2}t{3}{4}{5}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                ftpWorker.SendReportToFtp(ReportWriter.GetReportInfo(), string.Format("{0}_{1}.txt", reportUri, reportDate));

                Status = DBDownloader.Status.Stopped;
            }
        }

        public void CleanOperUp()
        {
            try
            {
                if (configuration.OperationalUpdateDirectory.Exists)
                {
                    InputMessage(string.Format("Trying to clean {0}", configuration.OperationalUpdateDirectory.FullName));
                    foreach (FileInfo file in configuration.OperationalUpdateDirectory.EnumerateFiles())
                    {
                        Log.WriteTrace("Delete file: {0}", file.FullName);
                        if(file.Exists) file.Delete();
                    }

                    string ftphost = ftpConfiguration.FtpSourcePath;
                    ftphost = ftphost.Remove(0, "ftp://".Length);
                    Log.WriteTrace("CleanOperUp - Host: {0}", ftphost);
                    FTPClient ftpClient = new FTPClient(ftphost, ftpConfiguration.User, ftpConfiguration.Password);
                    ftpClient.UseBinary = true;
                    if (useProxy)
                    {
                        ftpClient.ProxyAddress = proxyAddress;
                    }
                    ftpClient.UseBinary = false;
                    Log.WriteTrace("CleanOperUp - Get list directory: {0}", ftpConfiguration.ClearFolder);
                    foreach (var item in ftpClient.ListDirectory(ftpConfiguration.ClearFolder))
                    {
                        if (!item.IsDirectory)
                        {
                            InputMessage(string.Format("Downloading operup file: {0}", item.Name));
                            FileInfo destinationFile = new FileInfo(string.Format(@"{0}\{1}", configuration.OperationalUpdateDirectory.FullName, item.Name));
                            Uri sourceFile = new Uri(string.Format("{0}/{1}/{2}",ftpConfiguration.FtpSourcePath,  ftpConfiguration.ClearFolder, item.Name));
                            FTPDownloader itemDownloader = new FTPDownloader(networkCredential, destinationFile, sourceFile);
                            itemDownloader.UsePassiveFTP = configuration.UsePassiveFTP;
                            if (useProxy)
                            {
                                itemDownloader.UseProxy = true;
                                itemDownloader.ProxyAddress = proxyAddress;
                            }
                            itemDownloader.BeginAsync().Wait();
                        }
                    }
                    InputMessage(string.Format("Operup update finished"));
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("Deleting file error occurred: {0}", ex.Message);
                if (ex.InnerException != null)
                    Log.WriteError("Inner exception: {0}", ex.InnerException.Message);
            }
        }

        public void UpdateDownloadingItems()
        {
            if (loadingManager != null)
            {
                UpdateDownloadingStatus(loadingManager);
            }
        }

        private void UpdateDownloadingStatus(LoadingManager loadingManager)
        {
            downloadingItems.Clear();
            NNTD_DownloadingItems.Clear();
            foreach (LoadingManager.FileStatus fs in loadingManager.GetStatuses())
            {
                DownloadingItem di = new DownloadingItem();
                di.Title = fs.Title;
                di.FileName = fs.FileName;
                di.Status = fs.Status;
                di.Completion = fs.PercentOfComplete;
                di.FilesSize = string.Format("{0}/{1}",
                    fs.DestFileSize / 1024, fs.SourceFileSize / 1024);
                di.IsUpdateNedded = fs.IsUpdateNeeded;
                di.IsErrorOccured = fs.IsErrorOccured;
                di.ErrorMessage = fs.ErrorMessage;
                if (di.IsUpdateNedded)
                    downloadingItems.Add(di);
                else
                    NNTD_DownloadingItems.Add(di);
            }
        }

        public Task StartAsync()
        {
            Log.WriteInfo("DownloaderManager StartAsync {0}", DateTime.Now);
            if (Status == DBDownloader.Status.Stopped)
            {
                processingTask = Task.Factory.StartNew(Processing);
                return processingTask;
            }
            throw new Exception("Can't start new download until previous one ends.");
        }

        public void Stop()
        {
            if (Status == DBDownloader.Status.StartWaiting)
            {
                waitingStartCts.Cancel();
            }
            if (Status == DBDownloader.Status.InProgress && loadingManager != null)
            {
                Status = Status.Stopping;
                loadingManager.Cancel();
            }
        }

        private void InputMessage(string message)
        {
            Log.WriteTrace(message);
            SendConsoleMessage.Invoke(this, new StringEntryEventArgs(message));
        }

    }
}
