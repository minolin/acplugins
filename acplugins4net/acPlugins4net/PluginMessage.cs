using acPlugins4net.helpers;
using acPlugins4net.kunos;
using System;
using System.IO;
using System.Text;

namespace acPlugins4net
{
    public abstract class PluginMessage
    {
        public ACSProtocol.MessageType Type { get; protected internal set; }
        public DateTime CreationDate { get; protected internal set; }

        public PluginMessage(ACSProtocol.MessageType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            var s = "";
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop.Name != "StringRepresentation")
                    s += prop.Name + "=" + prop.GetValue(this, null) + Environment.NewLine;
            }
            return s;
        }

        protected internal abstract void Serialize(BinaryWriter bw);
        protected internal abstract void Deserialize(BinaryReader br);

        public TimestampedBytes ToBinary()
        {
            using (var m = new MemoryStream())
            using (var bw = new BinaryWriter(m))
            {
                bw.Write((byte)Type);
                Serialize(bw);
                return new TimestampedBytes(m.ToArray());
            }
        }

        [Obsolete("Used nowhere?")]
        public void FromBinary(TimestampedBytes data)
        {
            using (var m = new MemoryStream(data.RawData))
            using (var br = new BinaryReader(m))
            {
                var type = br.Read();
                if ((byte)Type != type)
                    throw new Exception("FromBinary() Type != type");

                Deserialize(br);
                CreationDate = data.IncomingDate;
            }
        }

        #region Helpers: (write & read binary stuff)

        protected static string readStringW(BinaryReader br)
        {
            // Read the length, 1 byte
            var length = br.ReadByte();

            // Read the chars
            return Encoding.UTF32.GetString(br.ReadBytes(length * 4));

        }

        protected static void writeStringW(BinaryWriter bw, string message)
        {
            bw.Write((byte)(message.Length));
            bw.Write(Encoding.UTF32.GetBytes(message));
        }

        protected static string readString(BinaryReader br)
        {
            // Read the length, 1 byte
            var length = br.ReadByte();

            // Read the chars
            return new string(br.ReadChars(length));

        }

        protected static void writeString(BinaryWriter bw, string message)
        {
            var array = message.ToCharArray();
            bw.Write((byte)array.Length);
            bw.Write(array);
        }

        protected static Vector3f readVector3f(BinaryReader br)
        {
            Vector3f res = new Vector3f();

            res.X = br.ReadSingle();
            res.Y = br.ReadSingle();
            res.Z = br.ReadSingle();

            return res;
        }

        protected static void writeVector3f(BinaryWriter bw, Vector3f vec)
        {
            bw.Write(vec.X);
            bw.Write(vec.Y);
            bw.Write(vec.Z);
        }

        #endregion
    }
}
