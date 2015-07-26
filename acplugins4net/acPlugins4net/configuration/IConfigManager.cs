using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.configuration
{
    public interface IConfigManager
    {
        string GetSetting(string key, string notnullmessage = "");
        int GetSettingAsInt(string key, string notnullmessage = "");
    }
}
