using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using DBDownloader.XML.Models.Autocomplects;
using DBDownloader.MainLogger;

namespace DBDownloader.XML
{
    public class AutoComplectsParser
    {
        private IEnumerable<FileInfo> files;
        private long clientCode;
        private List<AutoComplectList> autocomplects;
        public AutoComplectsParser(IEnumerable<FileInfo> files, long clientCode)
        {
            this.files = files;
            this.clientCode = clientCode;
            autocomplects = new List<AutoComplectList>();

            StreamReader streamReader = null;
            try
            {
                foreach (FileInfo file in files)
                {
                    if (file.Exists)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(AutoComplectList));
                        streamReader = new StreamReader(file.FullName);
                        autocomplects.Add(serializer.Deserialize(streamReader) as AutoComplectList);
                    }
                }
            }
            catch (InvalidOperationException ioEx)
            {
                Log.WriteError("AutoComplectsParser InvalidOperation Exception Occured: {0}", ioEx.Message);
                if (ioEx.InnerException != null)
                    Log.WriteError("AutoComplectsParser InvalidOperation Exception Occured: {0}", ioEx.InnerException.Message);
            }
            catch (Exception ex)
            {
                Log.WriteError("AutoComplectsParser Error Occured: {0}", ex.Message);
                if (ex.InnerException != null)
                    Log.WriteError("AutoComplectsParser Error Occured: {0}", ex.InnerException.Message);
            }
            finally
            {
                if (streamReader != null) streamReader.Close();
            }
        }

        public IList<AutoComplect> GetProductKeys()
        {
            List<AutoComplect> autoCompelecs = new List<AutoComplect>();
            foreach (AutoComplectList acList in autocomplects)
            {
                autoCompelecs.AddRange(acList.AutoComplects.Where(a => a.Ref == clientCode));
            }
            return autoCompelecs;
        }
    }
}
