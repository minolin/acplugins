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
                pluginManagerService.InteractiveStart();
                // Important note: Everything until pluginManagerService.InteractiveStop() won't happen for the services,
                // so please do only place console-related stuff here
                var input = GetBlockingInput();
                while (input != "x" && input != "exit" && pluginManager.IsConnected)
                {
                    // Basically we're blocking the Main Thread until exit.
                    // Ugly, but pretty easy to use by the deriving Plugin

                    // To have a bit of functionality we'll let the server admin 
                    // type in commands that can be understood by the deriving plugin
                    if (!string.IsNullOrEmpty(input))
                        pluginManager.ProcessEnteredCommand(input);

                    input = GetBlockingInput();
                }
                pluginManagerService.InteractiveStop();
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

        private static string GetBlockingInput()
        {
            // On a usual, interactive Console we'll just ask the user for input
            if(Environment.UserInteractive)
                return Console.ReadLine();

            // If not (and this is unfortunately always the case on linux, so we might consider a config option here)
            // we'll just pause for a while in order to get an artificial main loop
            System.Threading.Thread.Sleep(1000);
            return string.Empty;
        }
    }
}
