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
        private readonly string hrefPattern = @"<(A HREF|a href)="".*"">";
        private readonly string namePattern = @">[\s\d\w.]{1,}<";
        private readonly string lastdatePattern = @"\d{2}-\D*-\d{4} \d{2}:\d{2}:\d{2}";
        private readonly string sizePattern = @"\d{2}-\D*-\d{4} \d{2}:\d{2}:\d{2}(\r|\n|.)*\d*";
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

                    hrefValue = parseHref(line);
                    nameValue = parseFileName(line);

                    string couple = prepareCoupleString(line);

                    string dataString = parseLastModifiedDate(line);
                    lastModifiedValue = DateTime.Parse(dataString);

                    string sizeStr = couple.Substring(dataString.Length, couple.Length - dataString.Length);
                    sizeValue = long.Parse(sizeStr.Trim());

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

        private string RemoveSpaces(string line)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in line)
            {
                if (!char.IsWhiteSpace(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        public string parseHref(string line)
        {
            string hrefValue = string.Empty;
            Match result = Regex.Match(line, hrefPattern);
            if (result.Success)
            {
                int l = result.Value.Length;
                int skipL = 9;
                hrefValue = result.Value.Substring(skipL, l - skipL - 2);
                hrefValue = RemoveSpaces(hrefValue);
            }
            return hrefValue;
        }

        public string parseFileName(string line)
        {
            Match nameResult = Regex.Match(line, namePattern);
            if (nameResult.Success)
            {
                string nameValue = nameResult.Value;
                nameValue = nameValue.Substring(1, nameValue.Length - 2).Trim();
                return nameValue;
            }
            return string.Empty;
        }

        public string prepareCoupleString(string line)
        {
            Match sizeResult = Regex.Match(line, sizePattern);
            if (sizeResult.Success)
            {
                return sizeResult.Value;
            }
            return string.Empty;
        }

        public string parseLastModifiedDate(string line)
        {
            Match lastDateResult = Regex.Match(line, lastdatePattern);
            if (lastDateResult.Success)
            {
                return lastDateResult.Value;
            }
            return string.Empty;
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
