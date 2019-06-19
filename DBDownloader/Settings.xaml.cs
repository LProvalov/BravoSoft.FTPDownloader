using System;
using System.IO;
using System.Windows;

using DBDownloader.ConfigReader;

namespace DBDownloader
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        
        public object FileName { get; private set; }

        public Settings(string productVersion1Name, string productVersion2Name)
        {
            Configuration configuration = Configuration.GetInstance();
            InitializeComponent();
            if(configuration.RegFileInfo != null) regFilePathTextBox.Text = configuration.RegFileInfo.FullName;
            if(configuration.OperationalUpdateDirectory != null)
                oprationalUpdateTextBox.Text = configuration.OperationalUpdateDirectory.FullName;
            if (configuration.DBDirectory != null)
                dbdirectoryTextBox.Text = configuration.DBDirectory.FullName;
            trrCheckBox.IsChecked = configuration.IsTechnicalRegulationReform;
            /*
            if (configuration.ProductVersion == 0) productVersionRB1.IsChecked = true;
            else productVersionRB2.IsChecked = true;
            */
            DataContext = new SettingsPageViewModel() {
                ProductVersionRB1Content = productVersion1Name,
                ProductVersionRB2Content = productVersion2Name
            };
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Configuration configuration = Configuration.GetInstance();
            configuration.IsTechnicalRegulationReform = trrCheckBox.IsChecked.Value;
            /*
            if (productVersionRB1.IsChecked.Value) configuration.ProductVersion = 0;
            else configuration.ProductVersion = 1;
            */
            if (ValidateModel(configuration))
            {
                configuration.SaveConfiguration();
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Can't save settings without OperationalUpdateDirectory or RegFile", "Invalid settings", MessageBoxButton.OK);
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void regFilePathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Registration file (*.reg)|*.reg";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            bool? dialogResult = openFileDialog.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value == true)
            {
                Configuration.GetInstance().RegFileInfo = new FileInfo(openFileDialog.FileName);
                this.regFilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void operationalUpdateBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog(this.GetIWin32Window());
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Configuration.GetInstance().OperationalUpdateDirectory = new DirectoryInfo(folderDialog.SelectedPath);
                this.oprationalUpdateTextBox.Text = folderDialog.SelectedPath;
            }
        }

        private bool ValidateModel(Configuration configuration)
        {
            if ((configuration.OperationalUpdateDirectory != null && configuration.OperationalUpdateDirectory.Exists) ||
                (configuration.RegFileInfo != null && configuration.RegFileInfo.Exists) ||
                (configuration.DBDirectory != null && configuration.DBDirectory.Exists))
                return true;
            return false;
        }

        private void dbdirectoryBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog(this.GetIWin32Window());
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Configuration.GetInstance().DBDirectory = new DirectoryInfo(folderDialog.SelectedPath);
                this.dbdirectoryTextBox.Text = folderDialog.SelectedPath;
            }
        }
    }
}
