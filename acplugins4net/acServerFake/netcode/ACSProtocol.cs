using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.netcode
{
    /*
    static class ACSProtocol
    {
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


        internal struct Vector3f
        {
            public float x, y, z;

            public override string ToString()
            {
                return "[" + x.ToString() + " , " + y.ToString() + " , " + z.ToString() + "]";
            }
        }

        #region Quick&Dirty Message Encoding

        static void writeStringW(BinaryWriter bw, string message)
        {
            bw.Write((byte)(message.Length));
            bw.Write(Encoding.UTF32.GetBytes(message));
        }

        static void writeString(BinaryWriter bw, string message)
        {
            var b = Encoding.ASCII.GetBytes(message);
            bw.Write((byte)(b.Length));
            bw.Write(b);
        }

        static void writeVector3f(BinaryWriter bw, Vector3f vec)
        {
            bw.Write(vec.x);
            bw.Write(vec.y);
            bw.Write(vec.z);
        }

        private static byte[] EncodeNewSession(string name, byte type, UInt16 time, UInt16 laps, UInt16 waitTime, byte ambient_temp, byte road_temp, string weather)
        {
            using (var m = new MemoryStream())
            using (var bw = new BinaryWriter(m))
            {
                bw.Write(ACSProtocol.ACSP_NEW_SESSION);
                writeString(bw, name);
                bw.Write(type);
                bw.Write(time);
                bw.Write(laps);
                bw.Write(waitTime);
                bw.Write(ambient_temp);
                bw.Write(road_temp);
                writeString(bw, weather);

                return m.ToArray();
            }
        }


        private static byte[] EncodeCollisionCar(byte carId, byte otherCarId, float impactSpeed, Vector3f worldPosition, Vector3f relativePosition)
        {
            using (var m = new MemoryStream())
            using (var bw = new BinaryWriter(m))
            {
                bw.Write(ACSProtocol.ACSP_CLIENT_EVENT);
                bw.Write(ACSProtocol.ACSP_CE_COLLISION_WITH_CAR);
                bw.Write(carId);
                bw.Write(otherCarId);
                bw.Write(impactSpeed);
                writeVector3f(bw, worldPosition);
                writeVector3f(bw, relativePosition);

                return m.ToArray();
            }
        }


        private static byte[] EncodeCarInfo(byte carId, bool isConnected, string carModel, string carSkin, string driverName, string driverTeam, string driverGuid)
        {
            using (var m = new MemoryStream())
            using (var bw = new BinaryWriter(m))
            {
                bw.Write(ACSProtocol.ACSP_CAR_INFO);
                bw.Write(carId);
                bw.Write(isConnected);
                writeStringW(bw, carModel);
                writeStringW(bw, carSkin);
                writeStringW(bw, driverName);
                writeStringW(bw, driverTeam);
                writeStringW(bw, driverGuid);

                return m.ToArray();
            }
        }

        #endregion

    }
    */
}
