using System;
using System.IO;
using System.Text;

namespace acPlugins4net.helpers
{
    public class FileLogWriter : IFileLog
    {
        public static string GetExceptionString(Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            while (ex != null)
            {
                sb.Append("[");
                sb.Append(ex.GetType());
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.Append("]");
                sb.AppendLine();
                sb.Append(ex.StackTrace);
                sb.AppendLine();

                ex = ex.InnerException;
            }
            sb.AppendLine();

            return sb.ToString();
        }

        private readonly object lockObject = new object();

        private readonly string defaultLogDirectory;
        private StreamWriter log;
        private string currentFile;

        /// <summary>
        /// Let's you paste every line into the Console as well
        /// </summary>
        public bool CopyToConsole { get; set; } = false;

        /// <summary>
        /// Toggles logging with a Timestamp (no date)
        /// </summary>
        public bool LogWithTimestamp { get; set; } = false;

        public string CurrentLogFile
        {
            get { return currentFile; }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogWriter"/> class.
        /// </summary>
        /// <param name="defaultLogDirectory">If not null and the filePaths are relative the log files will be created relative to this directory</param>
        /// <param name="filePath">The file path. Can be relative or absolute. If null, StartLoggingToFile needs to be called before any logging takes place.</param>
        public FileLogWriter(string defaultLogDirectory = null, string filePath = null)
        {
            this.defaultLogDirectory = defaultLogDirectory;
            this.SetCurrentFile(filePath);
        }

        private void SetCurrentFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                this.currentFile = null;
            }
            else
            {
                if (Path.IsPathRooted(filePath))
                {
                    this.currentFile = filePath;
                }
                else if (string.IsNullOrEmpty(defaultLogDirectory))
                {
                    this.currentFile = Path.GetFullPath(filePath);
                }
                else
                {
                    this.currentFile = Path.Combine(defaultLogDirectory, filePath);
                }
            }
        }

        public virtual void StartLoggingToFile(string filePath)
        {
            lock (lockObject)
            {
                this.StopLoggingToFile();
                this.SetCurrentFile(filePath);
            }
        }

        public virtual void StopLoggingToFile()
        {
            lock (lockObject)
            {
                if (this.log != null)
                {
                    this.log.Close();
                    this.log.Dispose();
                    this.log = null;
                    this.currentFile = null;
                }
            }
        }

        public virtual void Log(string message)
        {
            this.LogMessageToFile(message);
        }

        public virtual void Log(Exception ex)
        {
            this.LogMessageToFile(GetExceptionString(ex));
        }

        protected void LogMessageToFile(string message)
        {
            lock (lockObject)
            {
                if (currentFile != null && this.log == null)
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(currentFile);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        this.log = new StreamWriter(currentFile);
                        this.log.AutoFlush = true;
                    }
                    catch (Exception ex)
                    {
                        currentFile = null;
                        Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
                if (this.log != null)
                {
                    if(LogWithTimestamp)
                        this.log.WriteLine(string.Format("{0} {1}", DateTime.Now.ToString("hh.mm.ss.ffffff"), message));
                    else
                        this.log.WriteLine(message);
                }
                if (CopyToConsole)
                {
                    Console.WriteLine(message);
                }
            }
        }
    }
}
