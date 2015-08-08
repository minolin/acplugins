namespace acPlugins4net.helpers
{
    public interface IFileLog : ILog
    {
        string CurrentLogFile { get; }
        void StartLoggingToFile(string filePath);
        void StopLoggingToFile();
    }
}