using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using DBDownloader.MainLogger;
using DBDownloader.Models;
using DBDownloader.ConfigReader;
using System.IO;

namespace DBDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [Flags]
        public enum ButtonsEnable
        {
            DISABLE_ALL = 1,
            Settings = 2,
            Start = 4,
            Schedule = 8,
            Stop = 16
        }

        public readonly double EXPANDED_ROW_HEIGHT = 120;
        public readonly double EXPANDED_NNTU_ROW_HEIGHT = 220;
        private readonly string CONFIG_FILE_STORAGE =
            string.Format(@"{0}", Directory.GetCurrentDirectory());

        private delegate void NoArgDelegate();
        private delegate void StringArgDelegate(string str);
        private delegate void StatusArgDelegate(Status status);
        
        //private Configuration configuration;
        private FtpConfiguration ftpConfiguration;
        private SchedulerModel schedulerModel;

        private DownloaderEngine _downloaderEngine;
        private DownloaderEngine DownloaderEngine
        {
            get
            {
                Configuration configuration = Configuration.GetInstance();
                if (_downloaderEngine == null) _downloaderEngine =
                        configuration.UseProxy ?
                        new DownloaderEngine(configuration, ftpConfiguration, schedulerModel, configuration.UseProxy, configuration.ProxyAddress) :
                        new DownloaderEngine(configuration, ftpConfiguration, schedulerModel);
                return this._downloaderEngine;
            }
        }

        private NoArgDelegate fetcher;

        public MainWindow()
        {
            Log.WriteInfo("MainWindow");

            Log.WriteTrace("Initialize main window components");
            InitializeComponent();

            this.Dispatcher.BeginInvoke(
                new NoArgDelegate(FirstStartInitialization),
                System.Windows.Threading.DispatcherPriority.Input,
                null);
        }

        private void FirstStartInitialization()
        {
            Log.WriteInfo("FirstStartInitialization");
            schedulerModel = new SchedulerModel();
            Configuration configuration = Configuration.GetInstance();
            configuration.LoadConfiguration();

            FileInfo configFile = new FileInfo(
                    string.Format(@"{0}\{1}", CONFIG_FILE_STORAGE, configuration.ConnectionInitFile));
            if (!configFile.Exists) throw new Exception("Config file does not found.");

            this.ftpConfiguration = new FtpConfiguration(configFile.FullName);
            if (configuration.DelayedStart > DateTime.Now)
            {
                schedulerModel.StartTime = configuration.DelayedStart;
            }

            DownloaderManagerInitialization();
            fetcher = new NoArgDelegate(this.FetchDownloaderManager);
            fetcher.BeginInvoke(null, null);

            if (!configuration.IsValid)
            {
                settingsWindowShow();
            }
            else
            {
                if (configuration.AutoStart)
                {
                    start();
                }
                else
                {
                    ButtonsEnableDisable(ButtonsEnable.Start | ButtonsEnable.Schedule | ButtonsEnable.Settings);
                }
            }
        }

        private void DownloaderManagerInitialization()
        {         
            DownloaderEngine.ProcessingEndedEvent += (obj, args) =>
            {
                this.mainGrid.Dispatcher.BeginInvoke(
                    new NoArgDelegate(() => { ButtonsEnableDisable(ButtonsEnable.Settings | ButtonsEnable.Start | ButtonsEnable.Schedule); }),
                    System.Windows.Threading.DispatcherPriority.Normal,
                    null);
            };

            DownloaderEngine.SendConsoleMessage += (obj, arg) =>
            {
                this.consoleTextBox.Dispatcher.BeginInvoke(
                    new StringArgDelegate(AddTextToConsole),
                    System.Windows.Threading.DispatcherPriority.Background,
                    arg.Text);
            };

            DownloaderEngine.StatusChangeEvent += (obj, args) =>
            {
                this.statusTextBlock.Dispatcher.BeginInvoke(
                    new StatusArgDelegate(UpdateStatus),
                    System.Windows.Threading.DispatcherPriority.Background,
                    args.Status);
            };
        }

        private void FetchDownloaderManager()
        {
            while (true)
            {
                Thread.Sleep(1033);
                downloadingList.Dispatcher.BeginInvoke(
                    new NoArgDelegate(UpdateDownloadingList),
                    System.Windows.Threading.DispatcherPriority.Background,
                    null);
            }
        }

        private void UpdateDownloadingList()
        {
            // TODO: change window controls update process.
            DownloaderEngine.UpdateDownloadingItems();
            downloadingList.ItemsSource = null;
            downloadingList.ItemsSource = DownloaderEngine.DownloadingItems;

            NNTL_downloadingList.ItemsSource = null;
            NNTL_downloadingList.ItemsSource = DownloaderEngine.NNTD_DownloadingItems;
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            settingsWindowShow();
        }
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            start();
        }
        private void start()
        {
            ButtonsEnableDisable(ButtonsEnable.Stop);

            DownloaderEngine.DelayedStart = Configuration.GetInstance().DelayedStart;
            DownloaderEngine.StartAsync();
        }

        private void scheduleButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteInfo("scheduler button click");
            //this.IsEnabled = false;
            Configuration configuration = Configuration.GetInstance();
            Scheduler schedulerWindow = new Scheduler(schedulerModel);
            schedulerWindow.Left = this.Left + this.Width / 2 - schedulerWindow.Width / 2;
            schedulerWindow.Top = this.Top + this.Height / 2 - schedulerWindow.Height / 2;
            bool? dialogResult = schedulerWindow.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                configuration.DelayedStart = DateTime.Now.AddMinutes(schedulerModel.DelayedMins);
                configuration.SaveConfiguration();
                ButtonsEnableDisable(ButtonsEnable.Stop);
                DownloaderEngine.DelayedStart = configuration.DelayedStart;
                DownloaderEngine.StartAsync();
            }
            else
            {
                configuration.DelayedStart = DateTime.Now;
                configuration.SaveConfiguration();
                ButtonsEnableDisable(ButtonsEnable.Settings|ButtonsEnable.Start|ButtonsEnable.Schedule);
            }
        }
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteInfo("stop button click");
            stopButton.IsEnabled = false;
            DownloaderEngine.Stop();
        }

        private void settingsWindowShow()
        {
            Log.WriteInfo("settingsWindowShow");
            this.IsEnabled = false;
            var items = this.ftpConfiguration.ProductModelItems;
            String name1 = "Version 1";
            String name2 = "Version 2";
            if (items.Count == 2)
            {
                name1 = items[0].ProductFileNameUI;
                name2 = items[1].ProductFileNameUI;
            }
            Settings settingsWindow = new Settings(name1, name2);
            settingsWindow.Left = this.Left + this.Width / 2 - settingsWindow.Width / 2;
            settingsWindow.Top = this.Top + this.Height / 2 - settingsWindow.Height / 2;
            bool? dialogResult = settingsWindow.ShowDialog();

            this.IsEnabled = true;
            if (Configuration.GetInstance().IsValid)
            {
                ButtonsEnableDisable(ButtonsEnable.Start | ButtonsEnable.Schedule | ButtonsEnable.Settings);
            }
            else
            {
                ButtonsEnableDisable(ButtonsEnable.Settings);
            }
        }

        private void expander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.expanderRow.Height = new GridLength(30);
        }
        private void expander_Expanded(object sender, RoutedEventArgs e)
        {
            this.expanderRow.Height = new GridLength(EXPANDED_ROW_HEIGHT);
        }

        private void ButtonsEnableDisable(ButtonsEnable buttonsEnable)
        {
            if ((buttonsEnable & ButtonsEnable.Settings) == ButtonsEnable.Settings)
            {
                settingsButton.IsEnabled = true;
            } else
            {
                settingsButton.IsEnabled = false;
            }

            if ((buttonsEnable & ButtonsEnable.Schedule) == ButtonsEnable.Schedule)
            {
                scheduleButton.IsEnabled = true;
            }
            else
            {
                scheduleButton.IsEnabled = false;
            }

            if ((buttonsEnable & ButtonsEnable.Start) == ButtonsEnable.Start)
            {
                startButton.IsEnabled = true;
            }
            else
            {
                startButton.IsEnabled = false;
            }

            if ((buttonsEnable & ButtonsEnable.Stop) == ButtonsEnable.Stop)
            {
                stopButton.IsEnabled = true;
            }
            else
            {
                stopButton.IsEnabled = false;
            }
        }

        private void AddTextToConsole(string text)
        {
            this.consoleTextBox.AppendText(string.Format("{0}\n", text));
        }

        private void UpdateStatus(Status status)
        {
            this.statusTextBlock.Text = status.ToString();
        }

        private void nntlExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.nntuExpanderRow.Height = new GridLength(45);
        }

        private void nntlExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.nntuExpanderRow.Height = new GridLength(EXPANDED_NNTU_ROW_HEIGHT);
        }

        private void consoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            consoleTextBox.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.WriteInfo("Close main window.");
            if (this.DownloaderEngine.Status != Status.Stopped)
            {
                string msg = "Происходит обновление данных, завершить?";
                MessageBoxResult result =
                  MessageBox.Show(
                    msg,
                    "Data App",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    Log.WriteInfo("Stopping downloading manager.");
                    DownloaderEngine.Stop();
                    while (DownloaderEngine.Status != Status.Stopped) Thread.Sleep(500);
                }
            }
        }
    }
}
