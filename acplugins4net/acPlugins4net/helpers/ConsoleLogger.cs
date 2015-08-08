using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.helpers
{
    class ConsoleLogger : ILog
    {
        private readonly string _logFilePath;

        public ConsoleLogger(string logFilePath = null)
        {
            _logFilePath = logFilePath;
        }

        void ILog.Log(Exception ex)
        {
            var msg = ex.Message;
            var iex = ex.InnerException;
            while(iex != null)
            {
                msg += Environment.NewLine + iex.Message;
                iex = iex.InnerException;
            }
            Console.WriteLine(msg);
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                using (var writer = System.IO.File.AppendText(_logFilePath))
                {
                    writer.WriteLine(msg);
                }
            }
        }

        void ILog.Log(string message)
        {
            Console.WriteLine(message);
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                using (var writer = System.IO.File.AppendText(_logFilePath))
                {
                    writer.WriteLine(message);
                }
            }
        }
    }
}
