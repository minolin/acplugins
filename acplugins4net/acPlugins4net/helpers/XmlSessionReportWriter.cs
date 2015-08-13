using acPlugins4net.info;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace acPlugins4net.helpers
{
    public class XmlSessionReportWriter : ISessionReportHandler
    {
        public void HandleReport(SessionInfo report)
        {
            string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sessionreports");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string filePath = Path.Combine(dir, new DateTime(report.Timestamp, DateTimeKind.Utc).ToString("yyyyMMdd_HHmmss") + "_" + report.TrackName + "_" + report.SessionName + ".xml");

            DataContractSerializer serializer = new DataContractSerializer(typeof(SessionInfo));

            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t"
            };

            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                serializer.WriteObject(writer, report);
            }
        }
    }
}