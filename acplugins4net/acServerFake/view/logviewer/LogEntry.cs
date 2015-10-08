using acPlugins4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace acServerFake.view.logviewer
{
    public class LogEntry : PropertyChangedBase
    {
        public DateTime DateTime { get; set; }

        private static int _indexCnter = 0;
        public int Index { get; private set; }

        public string Message { get; set; }

        public LogEntry()
        {
            Index = _indexCnter++;
        }
    }

    public class CollapsibleLogEntry : LogEntry
    {
        public List<LogEntry> Contents { get; set; }
    }

    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }
    }

    public class PluginMessageEntry : LogEntry
    {
        private PluginMessage _message = null;
        public string MsgType { get { return _message.Type.ToString(); } }
        public string DisplayShort { get { return _message.ToString().Replace(Environment.NewLine, "|"); } }
        public string DisplayMultiline { get { return _message.ToString(); } }
        public string Binary { get { return BitConverter.ToString(_message.ToBinary().RawData); } }

        public PluginMessageEntry(PluginMessage msg)
        {
            _message = msg;
        }
    }
}
