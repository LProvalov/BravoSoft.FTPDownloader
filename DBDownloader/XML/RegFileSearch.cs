using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DBDownloader.XML
{
    // Search and parse *.reg file, return distributor and client codes.
    public class RegFileSearch
    {
        private DirectoryInfo techCodexDir;
        private FileInfo regFileInfo;
        private long distributorCode;
        private long clientCode;

        public RegFileSearch(DirectoryInfo techCodexDir)
        {
            this.techCodexDir = techCodexDir;
            SearchAndParse();
        }

        public RegFileSearch(FileInfo regFileInfo)
        {
            this.regFileInfo = regFileInfo;
            SearchAndParse();
        }

        private bool SearchAndParse()
        {
            if (!(regFileInfo != null && regFileInfo.Exists))
            {
                if (techCodexDir != null && techCodexDir.Exists)
                {
                    FileInfo[] files;
                    files = techCodexDir.GetFiles("*.reg", SearchOption.AllDirectories);
                    foreach (FileInfo file in files)
                    {
                        if (TryParse(file)) return true;
                    }
                }
                return false;
            }
            else
            {
                return TryParse(regFileInfo);
            }
        }

        private bool TryParse(FileInfo file)
        {
            string fileName = file.Name.Remove(
                            file.Name.LastIndexOf(file.Extension), file.Extension.Length);

            string[] parsedStrings = fileName.Split('_');

            if (parsedStrings.Length < 2 ||
                !long.TryParse(parsedStrings[0], out distributorCode) ||
                !long.TryParse(parsedStrings[1], out clientCode)) return false;
            return true;
        }

        public long DistributorCode
        {
            get { return distributorCode; }
        }

        public long ClientCode
        {
            get { return clientCode; }
        }
    }
}
