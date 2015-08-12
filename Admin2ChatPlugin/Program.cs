using acPlugins4net;
using acPlugins4net.helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.messages;

namespace Admin2ChatPlugin
{
    public class Admin2ChatPlugin : AcServerPlugin
    {
        static void Main(string[] args)
        {
            try
            {
                AcServerPluginManager pluginManager = new AcServerPluginManager(new FileLogWriter(".", "log.txt") { CopyToConsole = true, LogWithTimestamp = true });
                pluginManager.LoadInfoFromServerConfig();
                pluginManager.AddPlugin(new Admin2ChatPlugin());
                pluginManager.LoadPluginsFromAppConfig();
                RunPluginInConsole.RunUntilAborted(pluginManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override bool OnCommandEntered(string cmd)
        {
            if (cmd.StartsWith("/chat "))
                PluginManager.BroadcastChatMessage(cmd.Replace("/chat ", ""));
            else if (cmd.StartsWith("/w "))
            {
                cmd = cmd.Replace("/w ", "").Trim();
                int car_id;
                string msg;

                #region Try to parse the car Id & text
                try
                {
                    var indexOfSpace = cmd.IndexOf(" ");
                    car_id = Convert.ToInt32(cmd.Substring(0, indexOfSpace));
                    msg = cmd.Substring(indexOfSpace);
                }
                catch (Exception)
                {
                    Console.WriteLine("Sorry, didn't get that. Please use /w {car_id} text, for example /w 2 hi car2");
                    return false;
                }
                #endregion

                PluginManager.SendChatMessage((byte)car_id, msg);
            }
            else if (cmd.StartsWith("/kick_id "))
            {
                Console.WriteLine("Try to kick with command: " + cmd);
                cmd = cmd.Replace("/kick_id ", "").Trim();
                int car_id;

                #region Try to parse the car Id & text
                try
                {
                    car_id = Convert.ToInt32(cmd.Trim());
                    Console.WriteLine("Car Id is " + car_id);
                }
                catch (Exception)
                {
                    Console.WriteLine("Sorry, didn't get that. Please use /kick_id {car_id} text, for example /kick_id 2");
                    return false;
                }

                PluginManager.RequestKickDriverById(Convert.ToByte(car_id));

                #endregion
            }
            else if (cmd.StartsWith("/info "))
            {
                cmd = cmd.Replace("/info ", "").Trim();
                int session_id;

                #region Try to parse the car Id & text
                try
                {
                    session_id = Convert.ToInt32(cmd.Trim());
                    Console.WriteLine("Car Id is " + session_id);
                }
                catch (Exception)
                {
                    Console.WriteLine("Sorry, didn't get that. Please use /info {index}, -1 for current session");
                    return false;
                }

                PluginManager.RequestSessionInfo(Convert.ToInt16(session_id));

                #endregion
            }
            else if (cmd.StartsWith("/set race2laps"))
            {
                var requestSetSession = last.CreateSetSessionRequest();
                requestSetSession.Laps = 2;
                requestSetSession.WaitTime = 15;
                PluginManager.RequestSetSession(requestSetSession);
            }

            return true;
        }

        protected override void OnChatMessage(MsgChat msg)
        {
            base.OnChatMessage(msg);
            Console.WriteLine("CHAT_" + msg.CarId + ": " + msg.Message);
        }


        MsgSessionInfo last = null;
        protected override void OnSessionInfo(MsgSessionInfo msg)
        {
            Console.WriteLine("SessionInfo: " + msg.ServerName + ", " + msg.Name + ", " + msg.ElapsedMS + ", index = " + msg.CurrentSessionIndex + ",  WaitTime = " + msg.WaitTime);
            last = msg;
        }
    }
}

