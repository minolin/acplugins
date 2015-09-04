using System;
using acPlugins4net.configuration;
using acPlugins4net.helpers;
using System.ServiceProcess;

namespace acPlugins4net
{
    public static class RunPluginInConsole
    {
        public static void RunUntilAborted(this AcServerPluginManager pluginManager)
        {
            if (pluginManager == null)
                throw new ArgumentNullException("pluginManager");

            var pluginManagerService = new RunPluginInConsoleServiceHelper(pluginManager);

            try
            {
                // If we're in some interactive console, we'll use Console.ReadLine() as usual
                // Just the start sequence has moved to the OnStart() function of the RunPluginInConsoleServiceHelper-Wrapper
                // This helps when the Plugin is started as Service or background task (especially linux)
                if (Environment.UserInteractive)
                {
                    pluginManagerService.InteractiveStart();
                    // Important note: Everything until pluginManagerService.InteractiveStop() won't happen for the services,
                    // so please do only place console-related stuff here
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
                    pluginManagerService.InteractiveStop();
                }
                // If not, we'll use it as a service and let the OS decide what to do when
                else
                {
                    ServiceBase.Run(pluginManagerService);
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
