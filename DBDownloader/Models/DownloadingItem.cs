using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBDownloader.Models
{
    public class DownloadingItem
    {
        public string Title { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }        
        public int Completion { get; set; }
        public string FilesSize { get; set; }
        public bool IsUpdateNedded { get; set; }
        public bool IsErrorOccured { get; set; }
        public string ErrorMessage { get; set; }
    }
}
