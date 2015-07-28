using acPlugins4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admin2ChatPlugin
{
    class Program : AcServerPlugin
    {
        static void Main(string[] args)
        {
            new Program().RunUntilAborted();
        }

        public override void OnConsoleCommand(string cmd)
        {
            if(cmd.StartsWith("/chat "))
                BroadcastChatMessage("ServerAdmin: " + cmd.Replace("/chat ", ""));
        }
    }
}
