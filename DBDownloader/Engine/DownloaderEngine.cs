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
using DBDownloader.MainLogger;
using DBDownloader.WinServices;
using System.Text;
using DBDownloader.Engine.Events;
using DBDownloader.LOG;
using DBDownloader.Providers;
using DBDownloader.Net;
using DBDownloader.Net.FTP;

namespace DBDownloader.Engine
{
    public enum Status
    {
        InProgress,
        Stopping,
        Stopped,
        StartWaiting
    }
    public class DownloaderEngine
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

        public event EventHandler ProcessingEndedEvent;

        private SchedulerModel scheduler;
        private RegFileSearch regFileSearch;

        private LoadingManager loadingManager;

        private NetworkCredential networkCredential;
        private DataProvider dataProvider;
        private bool useProxy = false;
        private string proxyAddress = string.Empty;
        private CancellationTokenSource waitingStartCts;

        private string reportUri;
        private readonly string REPORT_PATH = "report.txt";

        private readonly string AUTOCOMPLECTS_FILENAME_TEMPLATE = "AutoComplects_{0}.xml";
        private readonly string CONFIG_FILE_STORAGE =
            string.Format(@"{0}", Directory.GetCurrentDirectory());

        public DownloaderEngine(SchedulerModel scheduler,
            bool useProxy = false, string proxyAddress = "")
        {
            this.scheduler = scheduler;
            this.Status = Status.Stopped;
            this.useProxy = useProxy;
            this.proxyAddress = proxyAddress;
            this.dataProvider = new DataProvider();
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
                new FtpFilesProvider(networkCredential, useProxy, Configuration.GetInstance().UsePassiveFTP, proxyAddress) :
                new FtpFilesProvider(networkCredential, usePassiveFTP: Configuration.Instance.UsePassiveFTP);

            string autocomplectsFileName =
                string.Format(AUTOCOMPLECTS_FILENAME_TEMPLATE, regFileSearch.DistributorCode);

            Messenger.Instance.Write("autocomplect file downloading...", Messenger.Type.ApplicationBroadcast);
            FtpConfiguration ftpConfiguration = FtpConfiguration.Instance;

            FileInfo autoComplectFileInfo = ftpFilesProvider.GetFile(
                string.Format(@"{0}\{1}", CONFIG_FILE_STORAGE, autocomplectsFileName),
                string.Format(@"{0}/{1}", ftpConfiguration.AutocomplectsPath, autocomplectsFileName));

            Messenger.Instance.Write("Product files downloading...", Messenger.Type.ApplicationBroadcast);
            IEnumerable<FileInfo> productFiles =
                ftpFilesProvider.GetProductsFiles(CONFIG_FILE_STORAGE, ftpConfiguration);

            FileInfo[] autoComplectsFiles = new FileInfo[] { autoComplectFileInfo };
            Messenger.Instance.Write("Autocomplect file parsing...", Messenger.Type.ApplicationBroadcast);
            AutoComplectsParser autoComplectsParser =
                new AutoComplectsParser(autoComplectsFiles, regFileSearch.ClientCode);
            IList<AutoComplect> productIds = autoComplectsParser.GetProductKeys();
            productIds.Add(new AutoComplect() { Ref = regFileSearch.ClientCode, Kompl = 12000 });
            if (Configuration.Instance.IsTechnicalRegulationReform)
            {
                productIds.Add(new AutoComplect() { Ref = regFileSearch.ClientCode, Kompl = 0 });
            }

            Messenger.Instance.Write("Product files parsing...", Messenger.Type.ApplicationBroadcast);
            ProductsParser productParser = new ProductsParser(productFiles);
            string productListName = String.Empty;
            if (ftpConfiguration.ProductModelItems != null && ftpConfiguration.ProductModelItems.Count > 0)
            {
                productListName = ftpConfiguration.ProductModelItems[0].ProductFileName;
            } else
            {
                Log.WriteError("Product List Name doesn't define in FtpConfiguration!");
            }
            return productParser.GetDBList(productIds, productListName);
        }

        private void Initialize()
        {
            Messenger.Instance.Write("Download Engine Initializating...", Messenger.Type.ApplicationBroadcast | Messenger.Type.Log, Log.LogType.Info);
            regFileSearch = new RegFileSearch(Configuration.Instance.RegFileInfo);

            FtpConfiguration ftpConfiguration = FtpConfiguration.Instance;
            networkCredential =
               new NetworkCredential(ftpConfiguration.User, ftpConfiguration.Password);

            reportUri = string.Format(@"{0}//{1}/{2}",
                ftpConfiguration.FtpSourcePath, ftpConfiguration.ReportsPath, regFileSearch.ClientCode);
            string reportsDir = reportUri + ".txt";
            ReportWriter.AppendString("Клиент ID - {0}. {1}.\n", regFileSearch.ClientCode, DateTime.Now.ToLongTimeString());

            Messenger.Instance.Write("Loading manager initialization...", Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
            loadingManager = new LoadingManager(networkCredential, new Uri(reportsDir));
            loadingManager.ErrorOccurred += LoadingManager_ErrorOccurred;

            Log.WriteTrace("Initialize - ftp worker");

            string dbUriStr = string.Format(@"{0}//{1}",
                ftpConfiguration.FtpSourcePath, ftpConfiguration.DBPath);
            Dictionary<string, NetFileInfo> sizeDBDictionary = dataProvider.GetDBListWithSize(new Uri(dbUriStr));
            if (sizeDBDictionary.Count == 0) throw new Exception(
                string.Format("Can't get information about db files in current Url: {0}", dbUriStr));

            Log.WriteTrace("Initialize - get list of toms");
            Messenger.Instance.Write("Getting info about FTP DB files...", Messenger.Type.ApplicationBroadcast);
            List<string> missingFtpFiles = new List<string>();
            List<string> downloadedFtpFiles = new List<string>();

            foreach (Tom tom in GetListOfToms())
            {
                string destinationFileStr = string.Format(@"{0}\{1}",
                    Configuration.Instance.DBDirectory.FullName,
                    tom.FileName);
                string sourceFileUrl;
                if (String.IsNullOrEmpty(tom.FullPathname))
                {
                    sourceFileUrl = string.Format(@"{0}//{1}/{2}",
                        ftpConfiguration.FtpSourcePath,
                        ftpConfiguration.DBPath, tom.FileName);
                }
                else
                {
                    sourceFileUrl = string.Format(@"{0}//{1}/{2}",
                        ftpConfiguration.FtpSourcePath,
                        tom.FullPathname, tom.FileName);
                }
                long sourceSize = 0;
                bool isUpdateNeeded = false;
                DateTime creationFileDateTime = new DateTime();
                FileInfo destinationFile = new FileInfo(destinationFileStr);

                NetFileInfo sourceFileInfo;
                if (sizeDBDictionary.TryGetValue(sourceFileUrl, out sourceFileInfo))
                {
                    sourceSize = sourceFileInfo.Length;
                    creationFileDateTime = sourceFileInfo.LastModified;
                    if (!destinationFile.Exists) isUpdateNeeded = true;
                    else if (sourceFileInfo.LastModified.Date > destinationFile.CreationTime.Date ||
                        sourceFileInfo.LastModified.Date < destinationFile.CreationTime.Date && (sourceSize != 0 && destinationFile.Length != sourceSize))
                    {
                        Log.WriteTrace("File {0} needed to be updated, destDate :{1}, sourceDate :{2}, sourceSize: {3}, destLength: {4}",
                            tom.FileName, destinationFile.CreationTime.Date, sourceFileInfo.LastModified.Date, sourceSize, destinationFile.Length);
                        isUpdateNeeded = true;
                    }
                }
                else if (!String.IsNullOrEmpty(tom.FullPathname))
                {
                    var sourceFilePathString = string.Format(@"{0}//{1}",
                        ftpConfiguration.FtpSourcePath,
                        tom.FullPathname);
                    sizeDBDictionary = dataProvider.GetDBListWithSize(new Uri(sourceFilePathString));
                    if (sizeDBDictionary.TryGetValue(sourceFileUrl, out sourceFileInfo))
                    {
                        sourceSize = sourceFileInfo.Length;
                        creationFileDateTime = sourceFileInfo.LastModified;
                        if (!destinationFile.Exists) isUpdateNeeded = true;
                        else if (sourceFileInfo.LastModified.Date >= destinationFile.CreationTime.Date ||
                            sourceFileInfo.LastModified.Date < destinationFile.CreationTime.Date && (sourceSize != 0 && destinationFile.Length != sourceSize))
                        {
                            Log.WriteTrace("File {0} needed to be updated, destDate :{1}, sourceDate :{2}, sourceSize: {3}, destLength: {4}",
                                tom.FileName, destinationFile.CreationTime.Date, sourceFileInfo.LastModified.Date, sourceSize, destinationFile.Length);
                            isUpdateNeeded = true;
                        }
                    }
                    else
                    {
                        Log.WriteTrace("{0} no file on FTP", tom.FileName);
                        missingFtpFiles.Add(tom.FileName);
                        isUpdateNeeded = false;
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
            Messenger.Instance.Write(message, Messenger.Type.ApplicationBroadcast);
        }

        private bool DelayedStartFunc()
        {
            if (DelayedStart > DateTime.Now)
            {
                Status = DBDownloader.Engine.Status.StartWaiting;
                TimeSpan waitingTime = DelayedStart - DateTime.Now;
                using (waitingStartCts = new CancellationTokenSource())
                {
                    try
                    {
                        string message = string.Format("Delayed start {0}, waitingtime: {1}, Download will begin in: {2}",
                            DateTime.Now.ToLongTimeString(), waitingTime.ToString(), DateTime.Now.Add(waitingTime));
                        Messenger.Instance.Write(message, Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
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
                        Status = Status.Stopped;
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
                    Status = Status.InProgress;
                    if (loadingManager.DownloadingFileLenght > 0)
                    {
                        Log.WriteInfo("Trying to stop KTServices");
                        KTServices.Instance.Stop();
                        loadingManager.BeginAsync().Wait();
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = string.Format("Processing Error: Initialize error: {0}", ex.Message);
                    Log.WriteError(errorMessage);
                    Messenger.Instance.Write(errorMessage, Messenger.Type.ApplicationBroadcast);
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

                Log.WriteInfo("Trying to start KTServices");
                KTServices.Instance.Start();

                string reportDate = string.Format("d{0}{1}{2}t{3}{4}{5}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                dataProvider.SendReportToServer(ReportWriter.GetReportInfo(), string.Format("{0}_{1}.txt", reportUri, reportDate));

                SysInfo.SendSysInfoToFtp();

                Status = Status.Stopped;
            }
        }

        public void CleanOperUp()
        {
            try
            {
                Configuration configuration = Configuration.Instance;
                if (configuration.OperationalUpdateDirectory.Exists)
                {
                    Messenger.Instance.Write(string.Format("Trying to clean {0}", configuration.OperationalUpdateDirectory.FullName),
                                Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                    foreach (FileInfo file in configuration.OperationalUpdateDirectory.EnumerateFiles())
                    {
                        Log.WriteTrace("Delete file: {0}", file.FullName);
                        if (file.Exists) file.Delete();
                    }

                    //FtpConfiguration ftpConfiguration = FtpConfiguration.Instance;
                    //string ftphost = ftpConfiguration.FtpSourcePath;
                    //ftphost = ftphost.Remove(0, "ftp://".Length);
                    //Log.WriteTrace("CleanOperUp - Host: {0}", ftphost);
                    //FtpClient ftpClient = new FtpClient(ftphost, ftpConfiguration.User, ftpConfiguration.Password);
                    //ftpClient.UseBinary = true;
                    //if (useProxy)
                    //{
                    //    ftpClient.ProxyAddress = proxyAddress;
                    //}
                    //ftpClient.UseBinary = false;
                    //Log.WriteTrace("CleanOperUp - Get list directory: {0}", ftpConfiguration.ClearFolder);
                    //ftpClient.ListDirectory(ftpConfiguration.ClearFolder)
                    FileStruct[] listDirectory = dataProvider.ListDirectory(FtpConfiguration.Instance.ClearFolder);
                    foreach (var item in listDirectory)
                    {
                        if (!item.IsDirectory)
                        {
                            Messenger.Instance.Write(string.Format("Downloading operup file: {0}", item.Name),
                                Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                            FileInfo destinationFile = new FileInfo(string.Format(@"{0}\{1}", 
                                configuration.OperationalUpdateDirectory.FullName, 
                                item.Name));
                            Uri sourceFile = new Uri(string.Format("{0}/{1}/{2}", 
                                FtpConfiguration.Instance.FtpSourcePath, 
                                FtpConfiguration.Instance.ClearFolder, item.Name));
                            FTPDownloader itemDownloader = new FTPDownloader(FtpClient.CreateClient(), destinationFile, sourceFile);
                            itemDownloader.BeginAsync().Wait();
                        }
                    }
                    Messenger.Instance.Write(string.Format("Operup update finished"),
                        Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
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
            Log.WriteInfo("DownloaderEngine StartAsync {0}", DateTime.Now);
            if (Status == Status.Stopped)
            {
                processingTask = Task.Factory.StartNew(Processing);
                return processingTask;
            }
            throw new Exception("Can't start new download until previous one ends.");
        }

        public void Stop()
        {
            if (Status == Status.StartWaiting)
            {
                waitingStartCts.Cancel();
            }
            if (Status == Status.InProgress && loadingManager != null)
            {
                Status = Status.Stopping;
                loadingManager.Cancel();
            }
        }
    }
}
