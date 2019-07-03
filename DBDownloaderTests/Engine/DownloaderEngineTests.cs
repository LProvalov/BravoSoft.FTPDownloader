using Microsoft.VisualStudio.TestTools.UnitTesting;
using DBDownloader.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBDownloader.ConfigReader;

namespace DBDownloader.Engine.Tests
{
    [TestClass()]
    public class DownloaderEngineTests
    {
        [TestInitialize()]
        public void Setup()
        {
            Configuration configuration = Configuration.GetInstance();
            configuration.LoadConfiguration();
        }
        [TestMethod()]
        public void CleanOperUpTest()
        {
            try
            {
                DownloaderEngine dEngine = new DownloaderEngine(null);
                dEngine.CleanOperUp();
            } catch
            {
                Assert.Fail("Exception was throwed");
            }            
        }
    }
}