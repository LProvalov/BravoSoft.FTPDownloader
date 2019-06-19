using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DBDownloader.ViewModel
{
    public class NetSettingsViewModel : INotifyPropertyChanged
    {
        private short _netType;
        public short NetType {
            get { return _netType; }
            set {
                _netType = value;
                RaisePropertyChanged("NetType");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        protected void RaisePropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler temp = PropertyChanged;
            if (temp != null)
            {
                temp(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
