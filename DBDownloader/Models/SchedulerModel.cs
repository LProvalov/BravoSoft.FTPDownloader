using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBDownloader.Models
{
    public class SchedulerModel
    {
        public DateTime? StartTime { get; set; }
        public long DelayedMins { get; set; }
    }
}
