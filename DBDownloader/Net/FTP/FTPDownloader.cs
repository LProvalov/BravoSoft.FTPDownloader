﻿using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DBDownloader.MainLogger;

namespace DBDownloader.Net.FTP
{
    public class FTPDownloader
    {
        public enum FTPDownloaderStatus
        {
            stopped,
            stopping,
            inprogress,
            erroroccured,
            weberroroccured
        }

        private const int BUFFER_SIZE = 1024 * 512;

        private Uri sourceUri;
        private FileInfo destinationFileInfo;
        private FtpClient ftpClient = null;

        private long bytesDownloaded;

        private EventHandler cancelEventHandler;
        private CancellationTokenSource cancellationToken;
        private CancellationTokenSource loopCancellationTokenSource = null;

        private FtpStatusCode _ftpStatusCode;
        private bool deleteDestinationFile = false;

        public int DelayTime {get; set;} = 10000;
        public int RepeatCount {get; set;} = 10;

        public event ErrorEventHandler ErrorOccuredEvent;
        public event EventHandler DownloadEndEvent;
        public event EventHandler NextDownloadAttenptOccurred;

        public FTPDownloaderStatus Status { get; private set; }

        public bool IsErrorOccured { get; set; } = false;
        public string ErrorMessage { get; private set; } = string.Empty;
        public long BytesOfFileThatNeedToBeDownloaded { get; private set;}
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

        public class DateTimeEventArgs : EventArgs
        {
            private DateTime _creationDateTime;
            public DateTimeEventArgs(DateTime creationDateTime)
            {
                this._creationDateTime = creationDateTime;
            }
            public DateTime CreationFileDateTime { get { return _creationDateTime; } }
        }

        public FTPDownloader(
            FtpClient ftpClient,
            FileInfo destinationFileInfo, Uri sourceUri,
            long sourceSize = 0)
        {
            this.ftpClient = ftpClient;
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
                return ResumeFtpFileDownloadAsync(sourceUri, destinationFileInfo);
            }
            throw new ArgumentException("destinationFileInfo or credential cant be null");
        }

        public void Cancel()
        {
            Status = FTPDownloaderStatus.stopping;
            if (loopCancellationTokenSource != null)
                loopCancellationTokenSource.Cancel();
            if (cancelEventHandler != null)
                cancelEventHandler.Invoke(this, new EventArgs());
        }

        private void ResumeFtpFileDownload(Uri sourceUri, FileInfo destinationFile)
        {
            Status = FTPDownloaderStatus.inprogress;
            Log.WriteTrace("FTPDownloader - status inprogress");
            using (cancellationToken = new CancellationTokenSource())
            {
                FileStream localfileStream = null;
                FtpWebRequest request = ftpClient.CreateWebRequest(sourceUri, WebRequestMethods.Ftp.DownloadFile);
                request.UseBinary = true;

                WebResponse response = null;
                Stream responseStream = null;
                try
                {
                    if (destinationFile.Exists)
                    {
                        request.ContentOffset = destinationFile.Length;
                        Log.WriteTrace("Try to open local file: {0} for append, start from: {1}", destinationFile.FullName, destinationFile.Length);
                        localfileStream = new FileStream(destinationFile.FullName,
                            FileMode.Append, FileAccess.Write);
                    }
                    else
                    {
                        Log.WriteTrace("Try to create local file: {0}", destinationFile.FullName);
                        localfileStream = new FileStream(destinationFile.FullName,
                            FileMode.Create, FileAccess.Write);
                    }
                    Log.WriteTrace("File opened for writing");
                    response = request.GetResponse();
                    using (responseStream = response.GetResponseStream())
                    {
                        byte[] buffer = new byte[BUFFER_SIZE];
                        int bytesRead = responseStream.Read(buffer, 0, BUFFER_SIZE);

                        while (bytesRead != 0 && !cancellationToken.IsCancellationRequested)
                        {
                            localfileStream.Write(buffer, 0, bytesRead);
                            bytesDownloaded += bytesRead;
                            bytesRead = responseStream.Read(buffer, 0, BUFFER_SIZE);
                        }
                    }
                }
                catch (WebException wEx)
                {
                    String errorStatusDescription = ((FtpWebResponse)wEx.Response).StatusDescription;
                    _ftpStatusCode = ((FtpWebResponse)wEx.Response).StatusCode;
                    String errorStatus = ((FtpWebResponse)wEx.Response).StatusCode.ToString();
                    Log.WriteError("FTPDownloader - web error status {0}:{1}\nWeb error occurred:{2}", errorStatus, errorStatusDescription, wEx.Message);
                    if (errorStatus == "554") deleteDestinationFile = true;

                    ReportWriter.AppendString("Загрузка файла {0} - FAILED : {1}\n", sourceUri, wEx.Message);
                    if (wEx.InnerException != null)
                    {
                        Log.WriteTrace("FTPDownloader - inner Exception:{0}", wEx.InnerException.Message);
                        ReportWriter.AppendString("Inner Exception: {0}\n", wEx.InnerException.Message);
                    }

                    Status = FTPDownloaderStatus.weberroroccured;
                    IsErrorOccured = true;
                    ErrorMessage = wEx.Message;
                    if (ErrorOccuredEvent != null) ErrorOccuredEvent.BeginInvoke(this, new ErrorEventArgs(wEx), null, null);
                }
                catch (Exception ex)
                {
                    Log.WriteError("FTPDownloader - error occurred:{0}", ex.Message);
                    if (ex.InnerException != null)
                        Log.WriteTrace("FTPDownloader - inner Exception:{0}", ex.InnerException.Message);
                    Status = FTPDownloaderStatus.erroroccured;
                    IsErrorOccured = true;
                    ErrorMessage = ex.Message;
                    if (ErrorOccuredEvent != null )ErrorOccuredEvent.BeginInvoke(this, new ErrorEventArgs(ex), null, null);
                }
                finally
                {
                    try
                    {
                        if (response != null)
                        {
                            Log.WriteTrace("FTPDownloader - Closing response");
                            response.Close();
                        }
                        if (responseStream != null)
                        {
                            Log.WriteTrace("FTPDownloader - Closing resposeStream");
                            responseStream.Close();
                        }
                        if (localfileStream != null)
                        {
                            Log.WriteTrace("FTPDownloader - Closing localFileStream");
                            localfileStream.Close();
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (DownloadEndEvent != null) DownloadEndEvent.Invoke(this, new EventArgs());
                            ReportWriter.AppendString("Загрузка файла {0} - ОК\n", sourceUri);
                        }
                        destinationFile.Refresh();
                        Log.WriteTrace("FTPDownloader Destination file ({1}) length: {0}", destinationFile.Length, destinationFile.Name);
                        if (destinationFile.Exists && (destinationFile.Length == 0 || deleteDestinationFile))
                        {
                            File.Delete(destinationFile.FullName);
                        }
                    }
                    catch { }

                    if (Status != FTPDownloaderStatus.erroroccured && Status != FTPDownloaderStatus.weberroroccured)
                        Status = FTPDownloaderStatus.stopped;
                    Log.WriteTrace("FTPDownloader - stop downloading, status:{0}", Status);
                }
            }
            cancellationToken = null;
        }

        private Task ResumeFtpFileDownloadAsync(Uri sourceUri, FileInfo destinationFile)
        {
            return Task.Factory.StartNew(() =>
            {
                Log.WriteTrace("Start downloading: {0}", sourceUri.AbsoluteUri);
                BytesOfFileThatNeedToBeDownloaded = ftpClient.GetSourceFileSize(sourceUri);
                int loopCount = RepeatCount;
                do
                {
                    if (loopCount != RepeatCount && NextDownloadAttenptOccurred != null)
                    {
                        NextDownloadAttenptOccurred.BeginInvoke(this, new EventArgs(), null, null);
                    }
                    ResumeFtpFileDownload(sourceUri, destinationFile);
                    Log.WriteTrace("FTPDownloader status: {0}", Status);
                    if (Status == FTPDownloaderStatus.weberroroccured)
                    {
                        Log.WriteTrace("FTPDownloader WebError Occurred, wait {0}ms and repeat {1}", DelayTime, loopCount);
                        using (loopCancellationTokenSource = new CancellationTokenSource())
                        {
                            loopCancellationTokenSource.Token.WaitHandle.WaitOne(DelayTime);
                        }
                        loopCancellationTokenSource = null;
                        if (_ftpStatusCode != FtpStatusCode.ActionNotTakenFileUnavailable &&
                        _ftpStatusCode != FtpStatusCode.ActionNotTakenFilenameNotAllowed &&
                        _ftpStatusCode != FtpStatusCode.FileCommandPending)
                        {
                            loopCount--;
                        } else if (_ftpStatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            loopCount = 0;
                        }                            
                    }
                } while (Status == FTPDownloaderStatus.weberroroccured && loopCount > 0);
                Status = FTPDownloaderStatus.stopped;
            });
        }
    }
}
