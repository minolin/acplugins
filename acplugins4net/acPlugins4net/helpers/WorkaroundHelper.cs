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
                    string configFile = "";
                    try
                    {
                        var acDirectory = _Config.GetSetting("ac_server_directory", "Config-Setting 'ac_server_directory' is mandatory, but not set");
                        if (string.IsNullOrWhiteSpace(acDirectory))
                            throw new Exception("Config-Setting 'ac_server_directory' is mandatory, but not set (2)");

                        
                        configFile = Path.Combine(acDirectory, "cfg", "server_cfg.ini");
                        _ConfigIniLines = File.ReadAllLines(configFile);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("We have problems finding the server_cfg.ini file in '" + configFile + "'", ex);
                    }
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
            var line = ConfigIni.First(x => x.StartsWith(key));
            return line.Substring(line.IndexOf("=")+1).Trim();
        }
    }
}
