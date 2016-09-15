using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using DBDownloader.MainLogger;
using DBDownloader.Models;
using DBDownloader.ConfigReader;

namespace DBDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly double EXPANDED_ROW_HEIGHT = 120;
        public readonly double EXPANDED_NNTU_ROW_HEIGHT = 220;

        private delegate void NoArgDelegate();
        private delegate void StringArgDelegate(string str);
        private delegate void StatusArgDelegate(Status status);
        
        private Configuration configuration;
        private readonly string configurationPath = "AppConfig.xml";
        private SchedulerModel schedulerModel;

        private DownloaderManager _downloaderManager;
        private DownloaderManager DownloaderManager
        {
            get
            {
                if (_downloaderManager == null) _downloaderManager =
                        configuration.UseProxy ?
                        new DownloaderManager(configuration, schedulerModel, configuration.UseProxy, configuration.ProxyAddress) :
                        new DownloaderManager(configuration, schedulerModel);
                return this._downloaderManager;
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
            configuration = new Configuration(configurationPath);
            configuration.LoadConfiguration();

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
                    this.startButton.IsEnabled = true;
                    this.scheduleButton.IsEnabled = true;
                }
            }
        }

        private void DownloaderManagerInitialization()
        {
         
            DownloaderManager.ProcessingEndedEvent += (obj, args) =>
            {
                this.downloadingList.Dispatcher.BeginInvoke(
                    new NoArgDelegate(StopDownloading),
                    System.Windows.Threading.DispatcherPriority.Normal,
                    null);
            };

            DownloaderManager.SendConsoleMessage += (obj, arg) =>
            {
                this.consoleTextBox.Dispatcher.BeginInvoke(
                    new StringArgDelegate(AddTextToConsole),
                    System.Windows.Threading.DispatcherPriority.Background,
                    arg.Text);
            };

            DownloaderManager.StatusChangeEvent += (obj, args) =>
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
            DownloaderManager.UpdateDownloadingItems();
            downloadingList.ItemsSource = null;
            downloadingList.ItemsSource = DownloaderManager.DownloadingItems;

            NNTL_downloadingList.ItemsSource = null;
            NNTL_downloadingList.ItemsSource = DownloaderManager.NNTD_DownloadingItems;
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
            settingsButton.IsEnabled = false;
            startButton.IsEnabled = false;
            scheduleButton.IsEnabled = false;
            stopButton.IsEnabled = true;

            DownloaderManager.DelayedStart = configuration.DelayedStart;
            DownloaderManager.StartAsync();
        }

        private void scheduleButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteInfo("scheduler button click");
            this.IsEnabled = false;
            Scheduler schedulerWindow = new Scheduler(schedulerModel);
            schedulerWindow.Left = this.Left + this.Width / 2 - schedulerWindow.Width / 2;
            schedulerWindow.Top = this.Top + this.Height / 2 - schedulerWindow.Height / 2;
            bool? dialogResult = schedulerWindow.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                configuration.DelayedStart = DateTime.Now.AddMinutes(schedulerModel.DelayedMins);
                configuration.SaveConfiguration();
                this.IsEnabled = true;
                settingsButton.IsEnabled = false;
                startButton.IsEnabled = false;
                scheduleButton.IsEnabled = false;
                stopButton.IsEnabled = true;
                DownloaderManager.DelayedStart = configuration.DelayedStart;
                DownloaderManager.StartAsync();
            }
            else
            {
                configuration.DelayedStart = DateTime.Now;
                configuration.SaveConfiguration();
                this.IsEnabled = true;
                startButton.IsEnabled = true;
                scheduleButton.IsEnabled = true;
                stopButton.IsEnabled = false;
            }
        }
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteInfo("stop button click");
            stopButton.IsEnabled = false;
            DownloaderManager.Stop();
        }

        private void settingsWindowShow()
        {
            Log.WriteInfo("settingsWindowShow");
            this.IsEnabled = false;
            Settings settingsWindow = new Settings(configuration);
            settingsWindow.Left = this.Left + this.Width / 2 - settingsWindow.Width / 2;
            settingsWindow.Top = this.Top + this.Height / 2 - settingsWindow.Height / 2;
            bool? dialogResult = settingsWindow.ShowDialog();

            this.IsEnabled = true;
            if (configuration.IsValid)
            {
                this.startButton.IsEnabled = true;
                this.scheduleButton.IsEnabled = true;
            }
            else
            {
                this.startButton.IsEnabled = false;
                this.scheduleButton.IsEnabled = false;
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

        private void StopDownloading()
        {
            settingsButton.IsEnabled = true;
            startButton.IsEnabled = true;
            scheduleButton.IsEnabled = true;
            stopButton.IsEnabled = false;
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
            if (this.DownloaderManager.Status != Status.Stopped)
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
                    DownloaderManager.Stop();
                    while (DownloaderManager.Status != Status.Stopped) Thread.Sleep(500);
                }
            }
        }
    }
}
