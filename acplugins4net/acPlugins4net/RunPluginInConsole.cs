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
                pluginManager.Disconnect();
                pluginManager.Log("Disconnected...");
            }
        }

        public static void RunSinglePluginUntilAborted(this AcServerPluginBase plugin, ILog log, IConfigManager config = null, bool loadInfoFromServerConfig = true)
        {
            AcServerPluginManager pluginManager = new AcServerPluginManager(log, config);
            try
            {
                pluginManager.Log(plugin.PluginName + " starting up...");
                if (loadInfoFromServerConfig)
                {
                    pluginManager.Log("Loading info from server config...");
                    pluginManager.LoadInfoFromServerConfig();
                    pluginManager.Log("Track/Layout is " + pluginManager.Track + "[" + pluginManager.TrackLayout + "]");
                }
                pluginManager.AddPlugin(plugin);
            }
            catch (Exception ex)
            {
                pluginManager.Log("Fatal error during startup");
                pluginManager.Log(ex);
                // There's no point going into the main loop if the initialisation went horribly wrong
                return;
            }

            RunUntilAborted(pluginManager);
        }
    }
}
