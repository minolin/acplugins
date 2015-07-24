using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.kunos
{
    public class ACSProtocol
    {
        public enum MessageType : byte { // careful, we'll use byte as underlying datatype so we can just write the Type into the BinaryReader
            ERROR = 0,
            ACSP_NEW_SESSION = 50,
            ACSP_NEW_CONNECTION = 51,
            ACSP_CONNECTION_CLOSED = 52,
            ACSP_CAR_UPDATE = 53,
            ACSP_CAR_INFO = 54, // Sent as response to ACSP_GET_CAR_INFO command
            ACSP_LAP_COMPLETED = 73,

            // EVENTS
            ACSP_CLIENT_EVENT = 130,

            // EVENT TYPES
            ACSP_CE_COLLISION_WITH_CAR = 10,
            ACSP_CE_COLLISION_WITH_ENV = 11,

            // COMMANDS
            ACSP_REALTIMEPOS_INTERVAL = 200,
            ACSP_GET_CAR_INFO = 201,
            ACSP_SEND_CHAT = 202, // Sends chat to one car
            ACSP_BROADCAST_CHAT = 203, // Sends chat to everybody 
        }
    }
}
