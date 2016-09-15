using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DBDownloader.Models
{
    public class StatusInfo
    {
        private TextBox updatableTextbox;
        public StatusInfo(TextBox updatableTextbox)
        {
            this.updatableTextbox = updatableTextbox;
        }
        public string Text { get; set; }
    }
}
