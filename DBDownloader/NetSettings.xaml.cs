using DBDownloader.ConfigReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DBDownloader
{
    /// <summary>
    /// Interaction logic for NetSettings.xaml
    /// </summary>
    public partial class NetSettings : Window
    {
        public NetSettings()
        {
            InitializeComponent();
            switch(Configuration.Instance.NetClientType)
            {
                case 1: // Ftp
                    RBFtpType.IsChecked = true;
                    break;
                case 2: // Http
                    RBHttpType.IsChecked = true;
                    break;
                default:
                    RBFtpType.IsChecked = true;
                    break;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Configuration configuration = Configuration.Instance;
            configuration.NetClientType = 1; //default value is FTP
            if (RBFtpType.IsChecked.HasValue && RBFtpType.IsChecked.Value) configuration.NetClientType = 1;
            if (RBHttpType.IsChecked.HasValue && RBHttpType.IsChecked.Value) configuration.NetClientType = 2;
            configuration.SaveConfiguration();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}
