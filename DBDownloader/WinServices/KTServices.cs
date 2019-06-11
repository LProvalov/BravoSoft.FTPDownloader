using DBDownloader.ConfigReader;
using DBDownloader.Events;
using DBDownloader.LOG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.WinServices
{
    public class KTServices
    {
        private static KTServices _instance = null;
        public static KTServices Instance
        {
            get
            {
                if (_instance == null) _instance = new KTServices();
                return _instance;
            }
        }

        private bool isStopServicesNeeded = false;

        private KTServices()
        {

        }

        public void Stop()
        {
            foreach (string serviceName in Configuration.Instance.KTServices)
            {
                if (ServiceWorker.ServiceExists(serviceName) &&
                    ServiceWorker.GetServiceStatus(serviceName) ==
                        System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    Messenger.Instance.Write(string.Format("{0} trying to pause", serviceName),
                        Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                    if (ServiceWorker.PauseService(serviceName))
                    {
                        Messenger.Instance.Write(string.Format("{0} service paused", serviceName),
                            Messenger.Type.ApplicationBroadcast);
                        ReportWriter.AppendString("Остановка службы {0} - ОК\n", serviceName);
                        isStopServicesNeeded = true;
                    }
                    else
                    {
                        Messenger.Instance.Write(string.Format("Can't pause service, see logs for more information."),
                        Messenger.Type.ApplicationBroadcast);
                        ReportWriter.AppendString("Остановка службы {0} - FAILED\n", serviceName);
                    }
                }
                else
                {
                    Messenger.Instance.Write(string.Format("{0} service does not found or not running", serviceName),
                        Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                }
            }
        }

        public void Start()
        {
            Messenger.Instance.Write(string.Format("isStopServicesNeeded:{0}", isStopServicesNeeded),
                Messenger.Type.Log, MainLogger.Log.LogType.Info);
            if (isStopServicesNeeded)
            {
                isStopServicesNeeded = false;
                foreach (string serviceName in Configuration.Instance.KTServices)
                {
                    if (ServiceWorker.ServiceExists(serviceName))
                    {
                        var serviceStatus = ServiceWorker.GetServiceStatus(serviceName);
                        if (serviceStatus == System.ServiceProcess.ServiceControllerStatus.Stopped ||
                            serviceStatus == System.ServiceProcess.ServiceControllerStatus.Paused)
                        {
                            Messenger.Instance.Write(string.Format("{0} trying to start", serviceName),
                                Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                            if (ServiceWorker.ContinueService(serviceName))
                            {
                                Messenger.Instance.Write(string.Format("{0} service started", serviceName),
                                    Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                                ReportWriter.AppendString("Запуск службы {0} - ОК\n", serviceName);
                            }
                            else
                            {
                                Messenger.Instance.Write(string.Format("Can't start service, see logs for more information."),
                                    Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                                ReportWriter.AppendString("Запуск службы {0} - FAILED\n", serviceName);
                            }
                        }
                        else
                        {
                            Messenger.Instance.Write(string.Format("{0} service does not found or not stopped", serviceName),
                                    Messenger.Type.ApplicationBroadcast | Messenger.Type.Log);
                        }
                    }
                }
            }
        }
    }
}
