using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace acPlugins4net
{
    public class RunPluginInConsoleServiceHelper : ServiceBase
    {
        private AcServerPluginManager _pluginManager = null;
        public RunPluginInConsoleServiceHelper(AcServerPluginManager mgrToRun)
        {
            _pluginManager = mgrToRun;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            _pluginManager.Log("Connecting...");
            _pluginManager.Connect();
            _pluginManager.Log("... ok, we're good to go.");

        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        internal void InteractiveStart()
        {
            OnStart(new string[0]);
        }

        internal void InteractiveStop()
        {
            OnStop();
        }

    }
}
