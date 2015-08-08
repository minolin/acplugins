using System;
using acPlugins4net.configuration;
using acPlugins4net.helpers;

namespace acPlugins4net
{
    public static class RunPluginInConsole
    {
        public static void RunUntilAborted(this AcServerPluginManager pluginManager)
        {
            pluginManager.Log("Connecting...");
            pluginManager.Connect();
            pluginManager.Log("... ok, we're good to go.");

            var input = Console.ReadLine();
            while (input != "x" && input != "exit")
            {
                // Basically we're blocking the Main Thread until exit.
                // Ugly, but pretty easy to use by the deriving Plugin

                // To have a bit of functionality we'll let the server admin 
                // type in commands that can be understood by the deriving plugin
                if (!string.IsNullOrEmpty(input))
                    pluginManager.ProcessEnteredCommand(input);

                input = Console.ReadLine();
            }

            pluginManager.Disconnect();
            pluginManager.Log("Disconnected...");
        }

        [Obsolete("This method can be removed when switch to AcServerPluginManager is completed")]
        public static void RunUntilAborted(this AcServerPluginBase plugin)
        {
            IConfigManager config = new AppConfigConfigurator();
            RunSinglePluginUntilAborted(plugin, new ConsoleLogger("minoratingplugin.txt"));
        }

        public static void RunSinglePluginUntilAborted(this AcServerPluginBase plugin, ILog log, IConfigManager config = null, bool loadInfoFromServerConfig = true)
        {
            AcServerPluginManager pluginManager = new AcServerPluginManager(log, config);
            pluginManager.Log(plugin.PluginName + " starting up...");
            if (loadInfoFromServerConfig)
            {
                pluginManager.Log("Loading info from server config...");
                pluginManager.LoadInfoFromServerConfig();
                pluginManager.Log("Track/Layout is " + pluginManager.Track + "[" + pluginManager.TrackLayout + "]");
            }
            pluginManager.AddPlugin(plugin);
            RunUntilAborted(pluginManager);
        }
    }
}
