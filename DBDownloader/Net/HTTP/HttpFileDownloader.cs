using DBDownloader.LOG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBDownloader.Net.HTTP
{
    public sealed class HttpFileDownloader : NetFileDownloader
    {
        private HttpClient httpClient;
        public HttpFileDownloader(HttpClient httpClient, FileInfo destinationFileInfo, Uri sourceUri,
            long sourceSize = 0) : base(destinationFileInfo, sourceUri, sourceSize)
        {
            this.httpClient = httpClient;
        }

        private void DownloadFile(Uri sourceUri, FileInfo destinationFile)
        {

        }

        protected override Task DownloadFileAsync(Uri sourceUri, FileInfo destinationFile)
        {
            return Task.Factory.StartNew(() =>
            {
                Messenger.Instance.Write(string.Format("Start downloading: {0}", sourceUri),
                    Messenger.Type.Log, MainLogger.Log.LogType.Trace);
                int loopCount = RepeatCount;
                HttpStatusCode httpStatusCode = HttpStatusCode.OK;
                do
                {
                    if (loopCount != RepeatCount && nextDownloadAttemptOccuredEvent != null)
                    {
                        nextDownloadAttemptOccuredEvent.BeginInvoke(null, null);
                    }
                    Status = NetDownloaderStatus.inprogress;
                    using (cancellationToken = new CancellationTokenSource())
                    {
                        bool deleteDestinationFile = false;
                        try
                        {
                            httpClient.DownloadFile(sourceUri, destinationFile, cancellationToken);
                        }
                        catch (WebException wEx)
                        {
                            // TODO: Process web errors
                            httpStatusCode = ((HttpWebResponse)wEx.Response).StatusCode;
                            string errorStatus = httpStatusCode.ToString();

                            ReportWriter.AppendString("Загрузка файла {0} - FAILED : {1}\n", sourceUri, wEx.Message);
                            if (wEx.InnerException != null)
                            {
                                Messenger.Instance.Write(string.Format("HttpFileDownloader - inner Exception:{0}", wEx.InnerException.Message),
                                    Messenger.Type.Log, MainLogger.Log.LogType.Error);
                            }
                            Status = NetDownloaderStatus.weberroroccured;
                            IsErrorOccured = true;
                            ErrorMessage = wEx.Message;
                            if (errorOccuredEvent != null) errorOccuredEvent.BeginInvoke(new ErrorEventArgs(wEx), null, null);
                        }
                        catch (Exception ex)
                        {
                            // TODO: Process errors
                            Messenger.Instance.Write(string.Format("HttpFileDownloader exception: {0}", ex.Message),
                                    Messenger.Type.Log, MainLogger.Log.LogType.Error);
                        }
                        finally
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                if (downloadEndEvent != null) downloadEndEvent.Invoke();
                            }

                            destinationFile.Refresh();
                            if (destinationFile.Exists && (destinationFile.Length == 0 || deleteDestinationFile))
                            {
                                File.Delete(destinationFile.FullName);
                            }
                        }
                    }
                    cancellationToken = null;
                    if (Status == NetDownloaderStatus.weberroroccured)
                    {
                        Messenger.Instance.Write(string.Format("HttpDownloader WebError was occured, waiting {0}ms and repeat {1}", DelayTime, loopCount),
                            Messenger.Type.Log, MainLogger.Log.LogType.Trace);
                        using (loopCancellationTokenSource = new CancellationTokenSource())
                        {
                            loopCancellationTokenSource.Token.WaitHandle.WaitOne(DelayTime);
                        }
                        loopCancellationTokenSource = null;
                        if (httpStatusCode == HttpStatusCode.GatewayTimeout || 
                        httpStatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            loopCount--;
                        }
                        else
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
