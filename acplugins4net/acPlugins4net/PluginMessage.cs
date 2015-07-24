using acPlugins4net.kunos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net
{
    public abstract class PluginMessage
    {
        protected internal string NL = Environment.NewLine;
        public ACSProtocol.MessageType Type { get; private set; }
        public abstract string StringRepresentation { get; }

        public PluginMessage(ACSProtocol.MessageType type)
        {
            Type = type;
        }

        protected internal abstract void Serialize(BinaryWriter bw);
        protected internal abstract void Deserialize(BinaryReader br);

        public byte[] ToBinary()
        {
            using (var m = new MemoryStream())
            using (var bw = new BinaryWriter(m))
            {
                bw.Write((byte)Type);
                Serialize(bw);
                return m.ToArray();
            }
        }

        public void FromBinary(byte[] data)
        {
            using(var m = new MemoryStream(data))
            using (var br = new BinaryReader(m))
            {
                var type = br.Read();
                if ((byte)Type != type)
                    throw new Exception("FromBinary() Type != type");

                Deserialize(br);
            }
        }

        #region Helpers: (write & read binary stuff)

        public struct Vector3f
        {
            public float x, y, z;

            public override string ToString()
            {
                return "[" + x.ToString() + " , " + y.ToString() + " , " + z.ToString() + "]";
            }
        }

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
            var b = Encoding.ASCII.GetBytes(message);
            bw.Write((byte)(b.Length));
            bw.Write(b);
        }

        protected static Vector3f readVector3f(BinaryReader br)
        {
            Vector3f res = new Vector3f();

            res.x = br.ReadSingle();
            res.y = br.ReadSingle();
            res.z = br.ReadSingle();

            return res;
        }

        protected static void writeVector3f(BinaryWriter bw, Vector3f vec)
        {
            bw.Write(vec.x);
            bw.Write(vec.y);
            bw.Write(vec.z);
        }

        #endregion
    }
}
