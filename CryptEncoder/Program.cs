using System;
using System.Text;
using System.IO;
using Crypt;

namespace CryptEncoder
{
    class Program
    {
        static void Main(string[] args)
        {
            string ftpFileName = "FtpConfiguration.xml";
            string encodeFtpFileName = "FtpConfiguration.dat";
            Guid guid = new Guid("61613f97-d29e-4df1-8254-15ec61187b3c");
            byte[] key = guid.ToByteArray();
            CryptRC4 rc4Encoder = new CryptRC4(key);

            string ftpConfigStr = string.Empty;
            using (StreamReader streamReader = new StreamReader(ftpFileName))
            {
                ftpConfigStr = streamReader.ReadToEnd();
            }

            byte[] ftpConfigStrBytes = ASCIIEncoding.UTF8.GetBytes(ftpConfigStr);
            byte[] result = rc4Encoder.Encode(ftpConfigStrBytes, ftpConfigStrBytes.Length);
            File.WriteAllBytes(encodeFtpFileName, result);
        }
    }
}
