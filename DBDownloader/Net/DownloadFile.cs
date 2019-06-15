using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using DBDownloader.MainLogger;
using DBDownloader.Net.FTP;

namespace DBDownloader.Net
{
    public class DownloadFile
    {
        private FileInfo destinationFile;
        private FileInfo destinationFileCopy;
        private INetClient netClient;
        private Uri sourceFileUri;
        private bool isUpdateNeeded;
        private FTPDownloader downloader;
        //private bool useProxy;
        //private string proxyAddress;
        //private NetworkCredential credential;
        private DateTime creationFileDateTime;
        private bool downloadingEnd = false;
        private int repeatCount;
        private int delayTime;

        public event ErrorEventHandler errorEvent;

        public DownloadFile(/*NetworkCredential networkCredential,*/
            INetClient netClient,
            FileInfo destinationFile, Uri sourceFileUri,
            bool useProxy, string proxyAddress, bool usePassiveFTP,
            DateTime creationFileDateTime,
            bool isUpdateNeeded = true,
            long sourceSize = 0)
        {
            //this.credential = networkCredential;
            this.netClient = netClient;
            this.destinationFile = destinationFile;
            this.creationFileDateTime = creationFileDateTime;
            string fileName = destinationFile.Name.Remove(destinationFile.Name.IndexOf(destinationFile.Extension),
                destinationFile.Extension.Length);
            this.destinationFileCopy = new FileInfo(string.Format(@"{0}\{1}_copy{2}",
                destinationFile.DirectoryName, fileName, destinationFile.Extension));
            this.sourceFileUri = sourceFileUri;
            this.isUpdateNeeded = isUpdateNeeded;
            //this.useProxy = useProxy;
            //this.proxyAddress = proxyAddress;
            //downloader = new FTPDownloader(networkCredential, this.destinationFileCopy, this.sourceFileUri, sourceSize);
            downloader = new FTPDownloader(netClient as FtpClient, destinationFileCopy, sourceFileUri, sourceSize);
            //downloader.UseProxy = useProxy;
            //downloader.ProxyAddress = proxyAddress;
            //downloader.UsePassiveFTP = usePassiveFTP;
            downloader.DownloadEndEvent += OverwriteDestinationFile;
            downloader.ErrorOccuredEvent += ErrorEventOccurred;
        }

        public Task BeginAsync()
        {
            downloader.RepeatCount = repeatCount;
            downloader.DelayTime = delayTime;
            if (isUpdateNeeded) return downloader.BeginAsync();
            else return new Task(() => { });
        }

        private void OverwriteDestinationFile(object sender, EventArgs args)
        {
            try
            {
                Log.WriteTrace("OverwriteDestinationFile: {0} to {1}", destinationFileCopy.FullName, destinationFile.FullName);
                if (downloader.Status == FTPDownloader.FTPDownloaderStatus.inprogress)
                {
                    Log.WriteTrace("OverwriteDestinationFile downloader status: {0}", downloader.Status);
                    this.destinationFile.Refresh();
                    this.destinationFileCopy.Refresh();
                    if (destinationFile.Exists)
                    {
                        Log.WriteTrace("File exists, replace");
                        if (this.destinationFileCopy.Exists && this.destinationFileCopy.Length > 0)
                        {
                            File.Replace(this.destinationFileCopy.FullName, this.destinationFile.FullName,
                                null, false);
                            File.SetCreationTime(this.destinationFile.FullName, this.creationFileDateTime);
                            File.SetLastAccessTime(this.destinationFile.FullName, this.creationFileDateTime);
                            File.SetLastWriteTime(this.destinationFile.FullName, this.creationFileDateTime);
                            Log.WriteTrace("File successfuly replaced");
                        }
                    }
                    else
                    {
                        Log.WriteTrace("File not exists, move");
                        if (this.destinationFileCopy.Exists && this.destinationFileCopy.Length > 0)
                        {
                            Log.WriteTrace("File {0} move to {1}", destinationFileCopy.Name, destinationFile.FullName);
                            destinationFileCopy.MoveTo(destinationFile.FullName);
                            File.SetCreationTime(this.destinationFile.FullName, this.creationFileDateTime);
                            File.SetLastAccessTime(this.destinationFile.FullName, this.creationFileDateTime);
                            File.SetLastWriteTime(this.destinationFile.FullName, this.creationFileDateTime);
                            Log.WriteTrace("File successfuly moved");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("OverwriteDestinationFile ({0}) exception: {1}", destinationFile.FullName, ex.Message);
                if (ex.InnerException != null) Log.WriteError("Internal exception: {0}", ex.InnerException.Message);
            }
            finally
            {
                downloadingEnd = true;
            }
        }
        public FileInfo DestinationFile { get { return destinationFile; } }
        public long DestinationFileDownloadedLength
        {
            get
            {
                if (destinationFileCopy.Exists)
                { 
                    destinationFileCopy.Refresh();
                    return destinationFileCopy.Length;
                }
                if (destinationFile.Exists && downloadingEnd)
                {
                    destinationFile.Refresh();
                    return destinationFile.Length;
                }
                return 0;
            }
        }
        public Uri SourceFileUri { get { return sourceFileUri; } }
        public bool IsUpdateNeeded { get { return isUpdateNeeded; } }
        public FTPDownloader Downloader { get { return downloader; } }
        public string Title { get; set; }
        public int DelayTime
        {
            get { return delayTime; }
            set { delayTime = value; }
        }
        public int RepeatCount
        {
            get { return repeatCount; }
            set { repeatCount = value; }
        }
        public bool IsErrorOccured { get { return downloader.IsErrorOccured; } }
        public string ErrorMessage { get { return downloader.ErrorMessage; } }
        public void ErrorEventOccurred(object sender, ErrorEventArgs args)
        {
            if (errorEvent != null) errorEvent.BeginInvoke(this, args, null, null);
        }
    }
}
