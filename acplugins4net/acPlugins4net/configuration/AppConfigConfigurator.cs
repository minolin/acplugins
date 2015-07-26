using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.configuration
{
    class AppConfigConfigurator : IConfigManager
    {
        public string GetSetting(string key, string notnullmessage)
        {
            // The task: Get this setting out of the app.config and return it as T
            var value = ConfigurationManager.AppSettings[key];

            // If there is a not-null-message defined, and we didn't get a value we'll throw
            if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(notnullmessage))
                throw new Exception("Setting '" + key + "' is empty or wasn't found in the .config file, but:" + Environment.NewLine + notnullmessage);

            return value;
        }

        public int GetSettingAsInt(string key, string notnullmessage)
        {
            return Convert.ToInt32(GetSetting(key, notnullmessage));
        }
    }
}
