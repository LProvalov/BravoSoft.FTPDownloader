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
    public class SysInfoTests
    {
        [TestInitialize()]
        public void Setup()
        {
            Configuration configuration = Configuration.GetInstance();
            configuration.LoadConfiguration();
        }

        [TestMethod()]
        public void SendSysInfoToFtpTest()
        {
            try
            {
                SysInfo.SendSysInfoToFtp();
            } catch (Exception ex)
            {
                Assert.Fail("SysInfo failed.");
            }
        }
    }
}