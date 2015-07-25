using acPlugins4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace acServerFake.view.logviewer
{
    /// <summary>
    /// Wonderful piece of code by HighCore: http://stackoverflow.com/questions/16743804/implementing-a-log-viewer-with-wpf
    /// </summary>
    public partial class AwesomeViewerStolenFromTheInternet : UserControl
    {
        public ObservableCollection<LogEntry> LogEntries { get; set; }
        private static AwesomeViewerStolenFromTheInternet _sLastCreatedLogviewer = null;
        public static bool ReadyToShowErrorMessages { get { return _sLastCreatedLogviewer != null; } }

        public AwesomeViewerStolenFromTheInternet()
        {
            InitializeComponent();
            LogEntries = new ObservableCollection<LogEntry>();
            _sLastCreatedLogviewer = this;
            DataContext = LogEntries;
        }

        public void Append(LogEntry entry)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LogEntries.Add(entry)));
        }

        public void Clear()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LogEntries.Clear()));
        }

        public static void Log(PluginMessage msg)
        {
            if (_sLastCreatedLogviewer != null)
            {
                var entry = new PluginMessageEntry(msg);
                _sLastCreatedLogviewer.Append(entry);
            }
        }

        public static void CreateOutgoingLog(string msgType, string content, byte[] binary)
        {
            if (_sLastCreatedLogviewer != null)
            {
                var entry = new CollapsibleLogEntry()
                {
                    DateTime = DateTime.Now,
                    Message = Convert.ToString(msgType) + ": " + content
                };

                entry.Contents = new List<LogEntry>();
                entry.Contents.Add(new LogEntry()
                {
                    DateTime = DateTime.Now,
                    Message = BitConverter.ToString(binary)
                });

                _sLastCreatedLogviewer.Append(entry);
            }
        }

        internal static void LogException(Exception ex)
        {
            if (ex.InnerException != null)
                LogWithInner(ex);
            else
                LogAsSingleEntry(ex);
        }

        private static void LogAsSingleEntry(Exception ex)
        {
            var entry = new LogEntry()
            {
                DateTime = DateTime.Now,
                Message = "" + ex.GetType().Name + ": " + ex.Message
            };
            _sLastCreatedLogviewer.Append(entry);

        }

        private static void LogWithInner(Exception ex)
        {
            var entry = new CollapsibleLogEntry()
            {
                DateTime = DateTime.Now,
                Message = "" + ex.GetType().Name + ": " + ex.Message
            };

            if (ex.InnerException != null)
            {
                var errMsg = "";
                var inner = ex.InnerException;
                while (inner != null)
                {
                    errMsg += ex.Message + Environment.NewLine;
                    inner = inner.InnerException;
                }

                entry.Contents = new List<LogEntry>();
                entry.Contents.Add(new LogEntry()
                {
                    DateTime = DateTime.Now,
                    Message = errMsg
                });
            }

            _sLastCreatedLogviewer.Append(entry);
        }
    }
}
