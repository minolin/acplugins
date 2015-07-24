using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acRemoteServerUDP_Example {
    static class ACSProtocol {
        public const byte ACSP_NEW_SESSION = 50;
        public const byte ACSP_NEW_CONNECTION = 51;
        public const byte ACSP_CONNECTION_CLOSED = 52;
        public const byte ACSP_CAR_UPDATE = 53;
        public const byte ACSP_CAR_INFO = 54; // Sent as response to ACSP_GET_CAR_INFO command
        public const byte ACSP_LAP_COMPLETED = 73;

        // EVENTS
        public const byte ACSP_CLIENT_EVENT = 130;

        // EVENT TYPES
        public const byte ACSP_CE_COLLISION_WITH_CAR = 10;
        public const byte ACSP_CE_COLLISION_WITH_ENV = 11;

        // COMMANDS
        public const byte ACSP_REALTIMEPOS_INTERVAL = 200;
        public const byte ACSP_GET_CAR_INFO = 201;
        public const byte ACSP_SEND_CHAT = 202; // Sends chat to one car
        public const byte ACSP_BROADCAST_CHAT = 203; // Sends chat to everybody 
        
    }
}
