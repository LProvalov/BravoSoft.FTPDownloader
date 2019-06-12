using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Events
{
    public class StatusEntryArgs : EventArgs
    {
        private readonly Status _status;
        public StatusEntryArgs(Status status)
        {
            this._status = status;
        }
        public Status Status { get { return _status; } }
    }
}
