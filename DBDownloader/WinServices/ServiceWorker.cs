using System;
using System.Linq;
using System.ServiceProcess;

using DBDownloader.MainLogger;

namespace DBDownloader.WinServices
{
    public static class ServiceWorker
    {
        public static bool StartService(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running);
                return true;
            }
            catch(Exception ex)
            {
                Log.WriteError("Can't start service, error occurred: {0}", ex.Message);
                if (ex.InnerException != null)
                    Log.WriteError("Inner exception: {0}", ex.InnerException.Message);
                return false;
            }
        }

        public static bool ContinueService(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                service.Continue();
                service.WaitForStatus(ServiceControllerStatus.Running);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteError("Can't continue service, error occurred: {0}", ex.Message);
                if (ex.InnerException != null)
                    Log.WriteError("Inner exception: {0}", ex.InnerException.Message);
                return false;
            }
        }

        public static bool StopService(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
                return true;
            }
            catch(Exception ex)
            {
                Log.WriteError("Can't stop service, error occured: {0}", ex.Message);
                if(ex.InnerException != null)
                    Log.WriteError("Inner Exception: {0}", ex.InnerException.Message);
                return false;
            }
        }

        public static bool PauseService(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                service.Pause();
                service.WaitForStatus(ServiceControllerStatus.Paused);
                return true;
            }
            catch(Exception ex)
            {
                Log.WriteError("Can't pause service, error occured: {0}", ex.Message);
                if (ex.InnerException != null)
                    Log.WriteError("Inner Exception: {0}", ex.InnerException.Message);
                return false;
            }
        }

        public static ServiceControllerStatus GetServiceStatus(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            return service.Status;
        }

        public static bool ServiceExists(string serviceName)
        {
            ServiceController service = 
                ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(serviceName));
            if (service != null) return true;
            return false;                
        }
    }
}
