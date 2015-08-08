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
            new Program() { PluginName = "Admin2ChatPlugin" }.RunUntilAborted();
        }

        public override void OnConsoleCommand(string cmd)
        {
            if(cmd.StartsWith("/chat "))
                BroadcastChatMessage(cmd.Replace("/chat ", ""));
            else if(cmd.StartsWith("/w "))
            {
                cmd = cmd.Replace("/w ", "").Trim();
                int car_id;
                string msg;

                #region Try to parse the car Id & text
                try 
                {
                    var indexOfSpace = cmd.IndexOf(" ");
                    car_id = Convert.ToInt32( cmd.Substring(0, indexOfSpace));
                    msg = cmd.Substring(indexOfSpace);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Sorry, didn't get that. Please use /w {car_id} text, for example /w 2 hi car2");
                    return;
                }
#endregion

                SendChatMessage((byte)car_id, msg);
            }
        }
    }
}
