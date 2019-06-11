using DBDownloader.Events;
using DBDownloader.MainLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBDownloader.LOG
{
    public class Messenger
    {
        [Flags]
        public enum Type
        {
            Log = 1,
            ApplicationBroadcast = 2
        }
        private static Messenger _instance = null;
        public static Messenger Instance
        {
            get {
                if (_instance == null)
                {
                    _instance = new Messenger();
                }
                return _instance; }
        }

        public EventHandler<StringEntryEventArgs> MessageBroadcasted;

        public void Write(string message, Type msgType, Log.LogType logType = Log.LogType.Trace)
        {
            if ((msgType & Type.Log) == Type.Log) {
                switch(logType)
                {
                    case Log.LogType.Trace:
                        {
                            Log.WriteTrace(message);
                        } break;
                    case Log.LogType.Info:
                        {
                            Log.WriteInfo(message);
                        } break;
                    case Log.LogType.Error:
                        {
                            Log.WriteError(message);
                        } break;
                }
            }

            if ((msgType & Type.ApplicationBroadcast) == Type.ApplicationBroadcast)
            {
                if (MessageBroadcasted != null) MessageBroadcasted.BeginInvoke(this, new StringEntryEventArgs(message), null, null);
            }
        }

        private Messenger()
        {

        }
    }
}
