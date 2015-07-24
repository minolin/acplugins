using acPlugins4net.helpers;
using acPlugins4net.messages;
using acServerFake.netcode;
using acServerFake.view.logviewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    class NewSessionViewModel : BaseMessageViewModel
    {
        protected internal MsgNewSession _sessionEvent = null;

        #region New Session Properties

        public string Name
        {
            get { return _sessionEvent.Name; }
            set { _sessionEvent.Name = value; OnPropertyChanged("Name"); }
        }

        public byte SessionType
        {
            get { return _sessionEvent.SessionType; }
            set { _sessionEvent.SessionType = value; OnPropertyChanged("SessionType"); }
        }

        public ushort TimeOfDay
        {
            get { return _sessionEvent.TimeOfDay; }
            set { _sessionEvent.TimeOfDay = value; OnPropertyChanged("TimeOfDay"); }
        }

        public ushort Laps
        {
            get { return _sessionEvent.Laps; }
            set { _sessionEvent.Laps = value; OnPropertyChanged("Laps"); }
        }

        public ushort WaitTime
        {
            get { return _sessionEvent.WaitTime; }
            set { _sessionEvent.WaitTime = value; OnPropertyChanged("WaitTime"); }
        }

        public byte AmbientTemp
        {
            get { return _sessionEvent.AmbientTemp; }
            set { _sessionEvent.AmbientTemp = value; OnPropertyChanged("AmbientTemp"); }
        }

        public byte RoadTemp
        {
            get { return _sessionEvent.RoadTemp; }
            set { _sessionEvent.RoadTemp = value; OnPropertyChanged("RoadTemp"); }
        }

        public string Weather
        {
            get { return _sessionEvent.Weather; }
            set { _sessionEvent.Weather = value; OnPropertyChanged("Weather"); }
        }

        #endregion

        public override string MsgName
        {
            get { return "New Session"; }
        }

        public NewSessionViewModel(DuplexUDPClient UDPServer)
            : base(UDPServer)
        {
            _sessionEvent = new MsgNewSession(acPlugins4net.kunos.ACSProtocol.MessageType.ACSP_NEW_SESSION);

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

        public override byte[] GenerateBinaryCommand()
        {
            return _sessionEvent.ToBinary();
        }

        public override string CreateStringRepresentation()
        {
            return _sessionEvent.StringRepresentation;
        }
    }
}
