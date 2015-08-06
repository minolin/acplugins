using acPlugins4net.kunos;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.helpers
{
    public class AcMessageParser
    {
        public static PluginMessage Parse(byte[] rawData)
        {
            if (rawData == null)
                throw new ArgumentNullException("rawData");
            if (rawData.Length == 0)
                throw new ArgumentException("rawData is empty");

            ACSProtocol.MessageType msgType;
            try
            {
                msgType = (ACSProtocol.MessageType)rawData[0];
            }
            catch (Exception)
            {
                throw new Exception("Message contains unknown/not implemented Type-Byte '" + rawData[0] + "'");
            }

            PluginMessage newMsg = CreateInstance(msgType);
            using (var m = new MemoryStream(rawData))
            using (var br = new BinaryReader(m))
            {
                if (br.ReadByte() != (byte)newMsg.Type)
                    throw new Exception("Error in parsing the message, just because Minolin is dumb and you can't do anything about it");
                newMsg.Deserialize(br);
            }

            return newMsg;
        }

        [Obsolete("No longer needed when replaced AcServerPlugin with AcServerPluginNew")]
        internal static void Activate(AcServerPlugin acServerPlugin, byte[] data)
        {
            var msg = Parse(data);
            switch (msg.Type)
            {
                case ACSProtocol.MessageType.ACSP_NEW_SESSION:
                    acServerPlugin.OnNewSessionBase(msg as MsgNewSession);
                    break;
                case ACSProtocol.MessageType.ACSP_NEW_CONNECTION:
                    acServerPlugin.OnNewConnectionBase(msg as MsgNewConnection);
                    break;
                case ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED:
                    acServerPlugin.OnConnectionClosedBase(msg as MsgConnectionClosed);
                    break;
                case ACSProtocol.MessageType.ACSP_CAR_UPDATE:
                    acServerPlugin.OnCarUpdate(msg as MsgCarUpdate);
                    break;
                case ACSProtocol.MessageType.ACSP_CAR_INFO:
                    acServerPlugin.OnCarInfoBase(msg as MsgCarInfo);
                    break;
                case ACSProtocol.MessageType.ACSP_LAP_COMPLETED:
                    acServerPlugin.OnLapCompleted(msg as MsgLapCompleted);
                    break;
                case ACSProtocol.MessageType.ACSP_END_SESSION:
                    acServerPlugin.OnSessionEnded(msg as MsgSessionEnded);
                    break;
                case ACSProtocol.MessageType.ACSP_CLIENT_EVENT:
                    acServerPlugin.OnCollision(msg as MsgClientEvent);
                    break;
                case ACSProtocol.MessageType.ACSP_REALTIMEPOS_INTERVAL:
                case ACSProtocol.MessageType.ACSP_GET_CAR_INFO:
                case ACSProtocol.MessageType.ACSP_SEND_CHAT:
                case ACSProtocol.MessageType.ACSP_BROADCAST_CHAT:
                    throw new Exception("Received unexpected MessageType (for a plugin): " + msg.Type);
                case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR:
                case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_ENV:
                case ACSProtocol.MessageType.ERROR:
                default:
                    throw new Exception("Received wrong or unknown MessageType: " + msg.Type);
            }
        }

        private static PluginMessage CreateInstance(ACSProtocol.MessageType msgType)
        {
            switch (msgType)
            {
                case ACSProtocol.MessageType.ACSP_NEW_SESSION: return new MsgNewSession();
                case ACSProtocol.MessageType.ACSP_NEW_CONNECTION: return new MsgNewConnection();
                case ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED: return new MsgConnectionClosed();
                case ACSProtocol.MessageType.ACSP_CAR_UPDATE: return new MsgCarUpdate();
                case ACSProtocol.MessageType.ACSP_CAR_INFO: return new MsgCarInfo();
                case ACSProtocol.MessageType.ACSP_LAP_COMPLETED: return new MsgLapCompleted();
                case ACSProtocol.MessageType.ACSP_END_SESSION: return new MsgSessionEnded();
                case ACSProtocol.MessageType.ACSP_CLIENT_EVENT: return new MsgClientEvent();
                case ACSProtocol.MessageType.ACSP_REALTIMEPOS_INTERVAL: return new RequestRealtimeInfo();
                case ACSProtocol.MessageType.ACSP_GET_CAR_INFO: return new RequestCarInfo();
                case ACSProtocol.MessageType.ACSP_SEND_CHAT: return new RequestSendChat();
                case ACSProtocol.MessageType.ACSP_BROADCAST_CHAT: return new RequestBroadcastChat();
                case ACSProtocol.MessageType.ERROR:
                    throw new Exception("CreateInstance: MessageType is not set or wrong (ERROR=0)");
                case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR:
                case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_ENV:
                    throw new Exception("CreateInstance: MessageType " + msgType + " is not meant to be used as MessageType, but as Subtype to ACSP_CLIENT_EVENT");
                default:
                    throw new Exception("MessageType " + msgType + " is not known or implemented");
            }
        }
    }
}
