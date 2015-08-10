using System;
using acPlugins4net;
using acPlugins4net.helpers;
using acPlugins4net.messages;

namespace acServerFake.viemodel.messages
{
    class NewSessionViewModel : BaseMessageViewModel<MsgSessionInfo>
    {
        #region New Session Properties

        public string Name
        {
            get { return Message.Name; }
            set { Message.Name = value; OnPropertyChanged("Name"); }
        }

        public byte SessionType
        {
            get { return Message.SessionType; }
            set { Message.SessionType = value; OnPropertyChanged("SessionType"); }
        }

        public ushort TimeOfDay
        {
            get { return Message.TimeOfDay; }
            set { Message.TimeOfDay = value; OnPropertyChanged("TimeOfDay"); }
        }

        public ushort Laps
        {
            get { return Message.Laps; }
            set { Message.Laps = value; OnPropertyChanged("Laps"); }
        }

        public ushort WaitTime
        {
            get { return Message.WaitTime; }
            set { Message.WaitTime = value; OnPropertyChanged("WaitTime"); }
        }

        public byte AmbientTemp
        {
            get { return Message.AmbientTemp; }
            set { Message.AmbientTemp = value; OnPropertyChanged("AmbientTemp"); }
        }

        public byte RoadTemp
        {
            get { return Message.RoadTemp; }
            set { Message.RoadTemp = value; OnPropertyChanged("RoadTemp"); }
        }

        public string Weather
        {
            get { return Message.Weather; }
            set { Message.Weather = value; OnPropertyChanged("Weather"); }
        }

        #endregion

        public override string MsgCaption
        {
            get { return "New Session"; }
        }

        public NewSessionViewModel()
        {
            // Quick & dirty: Some defaults to speed up tests
            Name = "wtf is a session 'name'?";
            SessionType = 2;
            TimeOfDay = 233; // Ideas for a daytime format for a daytime in ushort? Minutes?
            Laps = 8;
            WaitTime = 30;
            AmbientTemp = 26;
            RoadTemp = 32;
            Weather = "clear"; // "erase" in german, haha
        }
    }
}
