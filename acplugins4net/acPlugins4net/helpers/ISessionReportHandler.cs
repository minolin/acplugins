using acPlugins4net.info;

namespace acPlugins4net.helpers
{
    public interface ISessionReportHandler
    {
        void HandleReport(SessionInfo report);
    }
}