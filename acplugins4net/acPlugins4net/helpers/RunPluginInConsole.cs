using acPlugins4net.configuration;
using acPlugins4net.helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net
{
    public static class RunPluginInConsole
    {
        public static void RunUntilAborted(AcServerPluginManager pluginManager)
        {
            pluginManager.Log.Log("Connecting...");
            pluginManager.Connect();
            pluginManager.Log.Log("... ok, we're good to go.");

            var input = Console.ReadLine();
            while (input != "x" && input != "exit")
            {
                // Basically we're blocking the Main Thread until exit.
                // Ugly, but pretty easy to use by the deriving Plugin

                // To have a bit of functionality we'll let the server admin 
                // type in commands that can be understood by the deriving plugin
                if (!string.IsNullOrEmpty(input))
                    pluginManager.OnConsoleCommand(input);

                input = Console.ReadLine();
            }

            pluginManager.Disconnect();
            pluginManager.Log.Log("Disconnected...");
        }

        public static void RunSinglePluginUntilAborted(IAcServerPlugin plugin, ILog log = null, IConfigManager config = null)
        {
            AcServerPluginManager pluginManager = new AcServerPluginManager(log, config);
            pluginManager.Log.Log(plugin.PluginName + " starting up...");
            pluginManager.AddPlugin(plugin);
            RunUntilAborted(pluginManager);
        }
    }
}
