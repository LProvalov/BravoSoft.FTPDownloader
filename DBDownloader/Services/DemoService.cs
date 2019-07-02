using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Services
{
    public class DemoService
    {
        private static DateTime availableTo = new DateTime(2019, 8, 1);

        public static string AvailableUntil()
        {
            return string.Format("application available until {0}", availableTo.ToLongDateString());
        }

        public static bool IsDemo()
        {
            return true;
        }
        public static bool IsAvailable()
        {
            if (DateTime.Now > availableTo) return true;
            return false;
        }
    }
}
