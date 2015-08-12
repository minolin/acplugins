using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace acPlugins4net.helpers
{
    [DataContract]
    public struct Vector3f
    {
        [DataMember]
        public float X { get; set; }
        [DataMember]
        public float Y { get; set; }
        [DataMember]
        public float Z { get; set; }

        public Vector3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static Vector3f operator +(Vector3f a, Vector3f b)
        {
            return new Vector3f(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3f operator -(Vector3f a, Vector3f b)
        {
            return new Vector3f(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public override string ToString()
        {
            return string.Format("[{0} , {1} , {2}]", X, Y, Z);
        }

        private static Random R = new Random();

        public static Vector3f RandomSmall()
        {
            return new Vector3f()
            {
                X = (float)(R.NextDouble() - 0.5) * 10,
                Y = (float)(R.NextDouble() - 0.5),
                Z = (float)(R.NextDouble() - 0.5) * 10,
            };
        }

        public static Vector3f RandomBig()
        {
            return new Vector3f()
            {
                X = (float)(R.NextDouble() - 0.5) * 1000,
                Y = (float)(R.NextDouble() - 0.5) * 20,
                Z = (float)(R.NextDouble() - 0.5) * 1000,
            };
        }
    }
}
