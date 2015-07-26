using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.configuration;
using System.IO;

namespace acPlugins4net.helpers
{
    class WorkaroundHelper
    {
        private IConfigManager _Config;
        private string[] _ConfigIniLines;

        public string[] ConfigIni
        {
            get
            {
                if (_ConfigIniLines == null)
                {
                    var configFile = Path.Combine(_Config.GetSetting("ac_server_directory"), "cfg", "server_cfg.ini");
                    _ConfigIniLines = File.ReadAllLines(configFile);
                }
                return _ConfigIniLines;
            }
        }

        public WorkaroundHelper(IConfigManager _Config)
        {
            this._Config = _Config;
        }

        internal string FindServerConfigEntry(string key)
        {
            var line = ConfigIni.First(x => x.StartsWith("TRACK="));
            return line.Substring(line.IndexOf("=")+1).Trim();
        }
    }
}
