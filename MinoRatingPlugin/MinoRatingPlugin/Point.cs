using System;

namespace MinoRatingPlugin
{
    public class Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point()
        {
        }

        public Point(float x, float z)
        {
            X = x;
            Y = z;
        }
    }
}
