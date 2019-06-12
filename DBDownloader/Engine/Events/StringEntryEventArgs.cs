using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBDownloader.Engine.Events
{
    public class StringEntryEventArgs : EventArgs
    {
        private readonly string str;
        public StringEntryEventArgs(string str)
        {
            this.str = str;
        }
        public string Text { get { return str; } }
    }
}
