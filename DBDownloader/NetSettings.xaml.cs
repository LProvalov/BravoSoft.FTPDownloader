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
                case Net.NetFileDownloader.NetClientTypes.FTP:
                    RBFtpType.IsChecked = true;
                    break;
                case Net.NetFileDownloader.NetClientTypes.HTTP:
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
            configuration.NetClientType = Net.NetFileDownloader.NetClientTypes.FTP; //default value is FTP
            if (RBFtpType.IsChecked.HasValue && RBFtpType.IsChecked.Value) configuration.NetClientType = Net.NetFileDownloader.NetClientTypes.FTP;
            if (RBHttpType.IsChecked.HasValue && RBHttpType.IsChecked.Value) configuration.NetClientType = Net.NetFileDownloader.NetClientTypes.HTTP;
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
