using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBDownloader.Net.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DBDownloader.Net.HTTP.Tests
{
    [TestClass()]
    public class KodupPageParserTests
    {
        KodupPageParser parser = new KodupPageParser();

        [TestMethod()]
        public void LoadTest()
        {
            using (StreamReader streamReader = new StreamReader(@"./Files/82.208.93.53_kodup.stream"))
            {
                parser.Load(streamReader);
                KodupPageFileStruct[] structs = parser.GetDatabaseFileList();
                var item1 = structs.FirstOrDefault(i => i.Name.Equals("41851.db6"));
                Assert.IsNotNull(item1);
                Assert.AreEqual(DateTime.Parse("29 - May - 2019 08:57:06"), item1.LastModified);
                Assert.AreEqual(64929, item1.Size);

                var item = structs.FirstOrDefault(i => i.Name.Equals("49531.db6"));
                Assert.IsNotNull(item);
                Assert.AreEqual(DateTime.Parse("31 - Oct - 2018 07:03:44"), item.LastModified);
                Assert.AreEqual(3675027361, item.Size);
            }
        }

        [TestMethod()]
        public void parseKodupDBListTest()
        {
            string line = @"		<A HREF="" / kodup / 41851.db6""><IMG SRC=""kodup / ~icon / unknown.gif"" WIDTH=32 HEIGHT=32 BORDER=0> 41851.db6</A>                       29-May-2019 08:57:06     64929 ";

            string href_result = parser.parseHref(line);
            Assert.AreEqual("/kodup/41851.db6", href_result, "Parse Href error");

            string dbname_result = parser.parseFileName(line);
            Assert.AreEqual("41851.db6", dbname_result, "Parse File Name error");
            
            string couple = parser.prepareCoupleString(line);
            string dataString = parser.parseLastModifiedDate(couple);
            Assert.AreEqual("29-May-2019 08:57:06", dataString, "Parse Last Modified Date error");

            string sizeStr = couple.Substring(dataString.Length, couple.Length - dataString.Length).Trim();
            Assert.AreNotEqual(string.Empty, sizeStr, "Database file size isn't correct");
            var sizeValue = long.Parse(sizeStr);
            Assert.AreEqual(64929L, sizeValue, "Parse File Size error");
        }
    }
}