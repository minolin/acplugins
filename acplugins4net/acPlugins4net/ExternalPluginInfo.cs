namespace acPlugins4net
{
    public class ExternalPluginInfo
    {
        public readonly string PluginName;
        public readonly int ListeningPort;
        public readonly string RemostHostname;
        public readonly int RemotePort;

        public ExternalPluginInfo(string pluginName, int listeningPort, string remostHostname, int remotePort)
        {
            PluginName = pluginName;
            ListeningPort = listeningPort;
            RemostHostname = remostHostname;
            RemotePort = remotePort;
        }
    }
}