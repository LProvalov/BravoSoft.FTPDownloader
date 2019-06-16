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
        private FileInfo destinationFileCopy;
        private INetClient netClient;
        private FTPDownloader downloader;
        private DateTime creationFileDateTime;
        private bool downloadingEnd = false;

        public string Title { get; set; }
        public int DelayTime { get; set; }
        public int RepeatCount { get; set; }
        public Uri SourceFileUri { get; private set; }
        public bool IsUpdateNeeded { get; private set; }
        public FileInfo DestinationFile { get; private set; }
        public event ErrorEventHandler errorEvent;

        public bool IsErrorOccured { get { return downloader.IsErrorOccured; } }
        public string ErrorMessage { get { return downloader.ErrorMessage; } }

        public DownloadFile(
            INetClient netClient,
            FileInfo destinationFile, Uri sourceFileUri,
            bool useProxy, string proxyAddress, bool usePassiveFTP,
            DateTime creationFileDateTime,
            bool isUpdateNeeded = true,
            long sourceSize = 0)
        {
            this.netClient = netClient;
            DestinationFile = destinationFile;
            this.creationFileDateTime = creationFileDateTime;
            string fileName = destinationFile.Name.Remove(destinationFile.Name.IndexOf(destinationFile.Extension),
                destinationFile.Extension.Length);
            destinationFileCopy = new FileInfo(string.Format(@"{0}\{1}_copy{2}",
                destinationFile.DirectoryName, fileName, destinationFile.Extension));
            SourceFileUri = sourceFileUri;
            IsUpdateNeeded = isUpdateNeeded;
            downloader = new FTPDownloader(netClient as FtpClient, destinationFileCopy, sourceFileUri, sourceSize);
            downloader.DownloadEndEvent += OverwriteDestinationFile;
            downloader.ErrorOccuredEvent += ErrorEventOccurred;
        }

        public Task BeginAsync()
        {
            downloader.RepeatCount = RepeatCount;
            downloader.DelayTime = DelayTime;
            if (IsUpdateNeeded) return downloader.BeginAsync();
            else return new Task(() => { });
        }

        private void OverwriteDestinationFile(object sender, EventArgs args)
        {
            try
            {
                Log.WriteTrace("OverwriteDestinationFile: {0} to {1}", destinationFileCopy.FullName, DestinationFile.FullName);
                if (downloader.Status == FTPDownloader.FTPDownloaderStatus.inprogress)
                {
                    Log.WriteTrace("OverwriteDestinationFile downloader status: {0}", downloader.Status);
                    DestinationFile.Refresh();
                    this.destinationFileCopy.Refresh();
                    if (DestinationFile.Exists)
                    {
                        Log.WriteTrace("File exists, replace");
                        if (this.destinationFileCopy.Exists && this.destinationFileCopy.Length > 0)
                        {
                            File.Replace(this.destinationFileCopy.FullName, DestinationFile.FullName,
                                null, false);
                            File.SetCreationTime(DestinationFile.FullName, this.creationFileDateTime);
                            File.SetLastAccessTime(DestinationFile.FullName, this.creationFileDateTime);
                            File.SetLastWriteTime(DestinationFile.FullName, this.creationFileDateTime);
                            Log.WriteTrace("File successfuly replaced");
                        }
                    }
                    else
                    {
                        Log.WriteTrace("File not exists, move");
                        if (this.destinationFileCopy.Exists && this.destinationFileCopy.Length > 0)
                        {
                            Log.WriteTrace("File {0} move to {1}", destinationFileCopy.Name, DestinationFile.FullName);
                            destinationFileCopy.MoveTo(DestinationFile.FullName);
                            File.SetCreationTime(DestinationFile.FullName, this.creationFileDateTime);
                            File.SetLastAccessTime(DestinationFile.FullName, this.creationFileDateTime);
                            File.SetLastWriteTime(DestinationFile.FullName, this.creationFileDateTime);
                            Log.WriteTrace("File successfuly moved");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("OverwriteDestinationFile ({0}) exception: {1}", DestinationFile.FullName, ex.Message);
                if (ex.InnerException != null) Log.WriteError("Internal exception: {0}", ex.InnerException.Message);
            }
            finally
            {
                downloadingEnd = true;
            }
        }
        
        public long DestinationFileDownloadedLength
        {
            get
            {
                if (destinationFileCopy.Exists)
                { 
                    destinationFileCopy.Refresh();
                    return destinationFileCopy.Length;
                }
                if (DestinationFile.Exists && downloadingEnd)
                {
                    DestinationFile.Refresh();
                    return DestinationFile.Length;
                }
                return 0;
            }
        }
        
        public void ErrorEventOccurred(object sender, ErrorEventArgs args)
        {
            if (errorEvent != null) errorEvent.BeginInvoke(this, args, null, null);
        }

        public void CancelDownloading()
        {
            if (downloader != null && downloader.Status == FTPDownloader.FTPDownloaderStatus.inprogress)
            {
                downloader.Cancel();
            }
        }

        public int GetPercentOfComplete()
        {
            if (downloader != null)
            {
                return downloader.PercentOfComplete;
            }
            return 0;
        }

        public long GetBytesOfFileThatNeedToBeDownloaded()
        {
            if (downloader != null)
            {
                return downloader.BytesOfFileThatNeedToBeDownloaded;
            }
            return 0L;
        }

        public FTPDownloader.FTPDownloaderStatus GetDownloaderStatus()
        {
            if (downloader != null) return downloader.Status;
            return FTPDownloader.FTPDownloaderStatus.stopped;
        }
    }
}
