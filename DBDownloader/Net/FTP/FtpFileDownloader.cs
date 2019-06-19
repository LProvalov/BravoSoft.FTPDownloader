using DBDownloader.MainLogger;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DBDownloader.Net.FTP
{
    public sealed class FtpFileDownloader : NetFileDownloader
    {
        private FtpClient _ftpClient = null;
        private FtpStatusCode _ftpStatusCode;
        private bool deleteDestinationFile = false;

        public FtpFileDownloader(FtpClient ftpClient,
            FileInfo destinationFileInfo, Uri sourceUri, 
            long sourceSize = 0) : base(destinationFileInfo, sourceUri, sourceSize)
        {
            this._ftpClient = ftpClient;
        }

        private void DownloadFile(Uri sourceUri, FileInfo destinationFile)
        {
            Status = NetDownloaderStatus.inprogress;
            Log.WriteTrace("FTPDownloader - status inprogress");
            using (cancellationToken = new CancellationTokenSource())
            {
                FileStream localfileStream = null;
                FtpWebRequest request = _ftpClient.CreateWebRequest(sourceUri, WebRequestMethods.Ftp.DownloadFile);
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

                    Status = NetDownloaderStatus.weberroroccured;
                    IsErrorOccured = true;
                    ErrorMessage = wEx.Message;
                    if (errorOccuredEvent != null) errorOccuredEvent.BeginInvoke(new ErrorEventArgs(wEx), null, null);
                }
                catch (Exception ex)
                {
                    Log.WriteError("FTPDownloader - error occurred:{0}", ex.Message);
                    if (ex.InnerException != null)
                        Log.WriteTrace("FTPDownloader - inner Exception:{0}", ex.InnerException.Message);
                    Status = NetDownloaderStatus.erroroccured;
                    IsErrorOccured = true;
                    ErrorMessage = ex.Message;
                    if (errorOccuredEvent != null) errorOccuredEvent.BeginInvoke(new ErrorEventArgs(ex), null, null);
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
                            if (downloadEndEvent != null) downloadEndEvent.Invoke();
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

                    if (Status != NetDownloaderStatus.erroroccured && Status != NetDownloaderStatus.weberroroccured)
                        Status = NetDownloaderStatus.stopped;
                    Log.WriteTrace("FTPDownloader - stop downloading, status:{0}", Status);
                }
            }
            cancellationToken = null;
        }

        protected override Task DownloadFileAsync(Uri sourceUri, FileInfo destinationFile)
        {
            return Task.Factory.StartNew(() =>
            {
                Log.WriteTrace("Start downloading: {0}", sourceUri.AbsoluteUri);
                BytesOfFileThatNeedToBeDownloaded = _ftpClient.GetSourceFileSize(sourceUri);
                int loopCount = RepeatCount;
                do
                {
                    if (loopCount != RepeatCount && nextDownloadAttemptOccuredEvent != null)
                    {
                        nextDownloadAttemptOccuredEvent.BeginInvoke(null, null);
                    }
                    DownloadFile(sourceUri, destinationFile);
                    Log.WriteTrace("FTPDownloader status: {0}", Status);
                    if (Status == NetDownloaderStatus.weberroroccured)
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
                        }
                        else if (_ftpStatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            loopCount = 0;
                        }
                    }
                } while (Status == NetDownloaderStatus.weberroroccured && loopCount > 0);
                Status = NetDownloaderStatus.stopped;
            });
        }
    }
}
