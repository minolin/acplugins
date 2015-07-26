using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.helpers
{
    class ConsoleLogger : ILog
    {
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
        }

        void ILog.Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
