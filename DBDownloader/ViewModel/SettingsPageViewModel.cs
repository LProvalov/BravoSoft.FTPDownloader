using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DBDownloader
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        private String _productVersionRB1Content;
        public String ProductVersionRB1Content
        {
            get { return _productVersionRB1Content; }
            set { this._productVersionRB1Content = value;
                RaisePropertyChanged("ProductVersionRB1Content");
            }
        }
        private String _productVersionRB2Content;
        public String ProductVersionRB2Content
        {
            get { return _productVersionRB2Content; }
            set
            {
                this._productVersionRB2Content = value;
                RaisePropertyChanged("ProductVersionRB2Content");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
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
