using System;
using acPlugins4net.configuration;
using acPlugins4net.helpers;

namespace acPlugins4net
{
    public static class RunPluginInConsole
    {
        public static void RunUntilAborted(this AcServerPluginManager pluginManager)
        {
            if (pluginManager == null)
                throw new ArgumentNullException("pluginManager");

            try
            {
                pluginManager.Log("Connecting...");
                pluginManager.Connect();
                pluginManager.Log("... ok, we're good to go.");

                var input = Console.ReadLine();
                while (input != "x" && input != "exit" && pluginManager.IsConnected)
                {
                    // Basically we're blocking the Main Thread until exit.
                    // Ugly, but pretty easy to use by the deriving Plugin

                    // To have a bit of functionality we'll let the server admin 
                    // type in commands that can be understood by the deriving plugin
                    if (!string.IsNullOrEmpty(input))
                        pluginManager.ProcessEnteredCommand(input);

                    input = Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                pluginManager.Log("Error in RunUntilAborted");
                pluginManager.Log(ex);
            }
            finally
            {
                if (pluginManager.IsConnected)
                {
                    pluginManager.Disconnect();
                }
                pluginManager.Log("Disconnected...");
            }
        }
    }
}
