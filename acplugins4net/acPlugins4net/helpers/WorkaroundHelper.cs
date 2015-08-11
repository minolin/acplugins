using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.configuration;
using System.IO;
using System.Reflection;

namespace acPlugins4net.helpers
{
    public class WorkaroundHelper
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
                        var acDirectory = _Config.GetSetting("ac_server_directory");
                        var configDir = _Config.GetSetting("ac_cfg_directory");
                        if (string.IsNullOrEmpty(configDir))
                            configDir = "cfg";

                        if (string.IsNullOrWhiteSpace(acDirectory))
                        {
                            acDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                            configFile = Path.Combine(acDirectory, configDir, "server_cfg.ini");

                            if (!File.Exists(configFile))
                            {
                                throw new Exception("Config-Setting 'ac_server_directory' is mandatory, but not set (2)");
                            }
                        }


                        configFile = Path.Combine(acDirectory, configDir, "server_cfg.ini");
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

        public string FindServerConfigEntry(string key)
        {
            string value;
            if (TryFindServerConfigEntry(key, out value))
            {
                return value;
            }

            throw new Exception("Config entry '" + key + "' not found in server_cfg.ini");
        }

        public bool TryFindServerConfigEntry(string key, out string value)
        {
            var line = ConfigIni.FirstOrDefault(x => x.StartsWith(key, StringComparison.InvariantCultureIgnoreCase));
            if (line != null)
            {
                value = line.Substring(line.IndexOf("=") + 1).Trim();
                return true;
            }
            value = null;
            return false;
        }
    }
}

