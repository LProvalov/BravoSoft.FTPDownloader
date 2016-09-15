using DBDownloader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for Scheduler.xaml
    /// </summary>
    public partial class Scheduler : Window
    {
        private SchedulerModel model;
        private long delayedMins;
        public Scheduler(SchedulerModel model)
        {
            this.model = model;
            InitializeComponent();
            delayedTimeTextBox.Text = model.DelayedMins.ToString();
            //if (model.StartTime.HasValue)
            //    dateTimePicker.Value = model.StartTime.Value;
            //dateTimePicker.Minimum = DateTime.Now;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            //if(dateTimePicker.Value.HasValue)
            //    model.StartTime = dateTimePicker.Value.Value;
            
            model.DelayedMins = delayedMins;
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            model.StartTime = null;
            model.DelayedMins = 0;
            this.DialogResult = false;
            this.Close();
        }

        private void delayedTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (delayedTimeTextBox == null) return;
            if (!long.TryParse(delayedTimeTextBox.Text, out delayedMins))
                delayedTimeTextBox.Text = delayedMins.ToString();
        }
    }
}
