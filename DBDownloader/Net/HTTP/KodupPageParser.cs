using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBDownloader.Net.HTTP
{
    public class KodupPageParser
    {
        private readonly int SKIP_HTML_LINE = 5;
        private readonly string hrefPattern = @"<A HREF="".*"">";
        private readonly string namePattern = @"\b>.*<";
        private readonly string lastdatePattern = @"\d{2}-\D*-\d{4} \d{2}:\d{2}:\d{2}";
        private readonly string sizePattern = @"\d{2}-\D*-\d{4} \d{2}:\d{2}:\d{2}.?\d*";
        private List<KodupPageFileStruct> databaseFilesList = new List<KodupPageFileStruct>();

        public void Load(StreamReader readStream)
        {
            int skipCount = SKIP_HTML_LINE;
            while (readStream.Peek() >= 0)
            {
                string line = readStream.ReadLine().Trim();
                if (skipCount > 0)
                {
                    skipCount--;
                    continue;
                }
                try
                {
                    string hrefValue = string.Empty;
                    string nameValue = string.Empty;
                    DateTime lastModifiedValue = new DateTime(0);
                    long sizeValue = 0;
                    Match hrefResult = Regex.Match(line, hrefPattern);
                    if (hrefResult.Success)
                    {
                        var l = hrefResult.Value.Length;
                        int skipL = 9;
                        hrefValue = hrefResult.Value.Substring(skipL, l - skipL - 2);
                    }

                    Match nameResult = Regex.Match(line, namePattern);
                    if (nameResult.Success)
                    {
                        nameValue = nameResult.Value;
                        nameValue = nameValue.Substring(1, nameValue.Length - 2).Trim();
                    }

                    Match sizeResult = Regex.Match(line, sizePattern);
                    if (sizeResult.Success)
                    {
                        string couple = sizeResult.Value;
                        Match lastDateResult = Regex.Match(couple, lastdatePattern);
                        if (lastDateResult.Success)
                        {
                            lastModifiedValue = DateTime.Parse(lastDateResult.Value);
                            var start = lastDateResult.Value.Length;
                            var length = couple.Length - lastDateResult.Value.Length;
                            string sizeStr = couple.Substring(start, length);
                            sizeValue = long.Parse(sizeStr.Trim());
                        }
                    }

                    databaseFilesList.Add(new KodupPageFileStruct()
                    {
                        Name = nameValue,
                        HREF = hrefValue,
                        Size = sizeValue,
                        LastModified = lastModifiedValue
                    });
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
        }

        public KodupPageFileStruct[] GetDatabaseFileList()
        {
            return databaseFilesList.ToArray();
        }
    }

    public struct KodupPageFileStruct
    {
        public string HREF;
        public string Name;
        public DateTime LastModified;
        public double Size;
    }
}
