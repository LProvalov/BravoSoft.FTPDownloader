using DBDownloader.Net.FTP;
using DBDownloader.Net.HTTP;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DBDownloader.Net
{
    public abstract class NetFileDownloader
    {
        public enum NetDownloaderStatus
        {
            stopped,
            stopping,
            inprogress,
            erroroccured,
            weberroroccured
        }
        public enum NetClientTypes
        {
            FTP = 0,
            HTTP = 1
        }

        public class NetFileDownloaderException : Exception
        {
            public NetFileDownloaderException(string message) : base(message)
            {
            }
        }

        public static NetFileDownloader CreateFileDownloader(INetClient netClient, FileInfo destinationFile, Uri sourceUri, long sourceSize = 0)
        {
            if (netClient is FtpClient)
                return new FtpFileDownloader(netClient as FtpClient, destinationFile, sourceUri, sourceSize);
            if (netClient is HttpClient)
                return new HttpFileDownloader(netClient as HttpClient, destinationFile, sourceUri, sourceSize);
            throw new ArgumentException("Unknown net client type");
        }

        protected Uri sourceUri;
        protected FileInfo destinationFileInfo;

        protected const int BUFFER_SIZE = 1024 * 80;
        protected long bytesDownloaded = 0;

        protected EventHandler cancelEventHandler;
        protected CancellationTokenSource cancellationToken;
        protected CancellationTokenSource loopCancellationTokenSource = null;

        public int DelayTime { get; set; } = 10000;
        public int RepeatCount { get; set; } = 10;

        public bool IsErrorOccured { get; set; } = false;
        public string ErrorMessage { get; protected set; } = string.Empty;

        public delegate void SimpleDelegate();
        public delegate void ErrorEventDeletage(ErrorEventArgs args);
        public SimpleDelegate nextDownloadAttemptOccuredEvent;
        public SimpleDelegate downloadEndEvent;
        public ErrorEventDeletage errorOccuredEvent;

        public NetDownloaderStatus Status { get; protected set; }
        public long BytesOfFileThatNeedToBeDownloaded { get; protected set; }
        public int PercentOfComplete
        {
            get
            {
                long already = 0;
                if (destinationFileInfo != null)
                {
                    destinationFileInfo.Refresh();
                    already = destinationFileInfo.Exists ? destinationFileInfo.Length : 0;
                }
                return BytesOfFileThatNeedToBeDownloaded != 0 ?
                    (int)((double)(bytesDownloaded + already) / BytesOfFileThatNeedToBeDownloaded * 100) : 0;
            }
        }

        protected NetFileDownloader(FileInfo destinationFileInfo, Uri sourceUri,
            long sourceSize = 0)
        {
            this.sourceUri = sourceUri;
            this.destinationFileInfo = destinationFileInfo;
            BytesOfFileThatNeedToBeDownloaded = sourceSize;
            cancelEventHandler += (sender, obj) =>
            {
                if (cancellationToken != null) cancellationToken.Cancel();
            };
        }

        public Task BeginAsync()
        {
            if (destinationFileInfo != null)
            {
                return DownloadFileAsync(sourceUri, destinationFileInfo);
            }
            throw new ArgumentException("DestinationFileInfo can't be null");
        }

        public void Cancel()
        {
            Status = NetDownloaderStatus.stopping;
            if (loopCancellationTokenSource != null)
                loopCancellationTokenSource.Cancel();
            if (cancelEventHandler != null)
                cancelEventHandler.Invoke(this, new EventArgs());
        }

        protected abstract Task DownloadFileAsync(Uri sourceUri, FileInfo destinationFile);
    }
}
