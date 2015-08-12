using acPlugins4net.helpers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace acPlugins4net.info
{
    [DataContract]
    public class DriverInfo
    {
        public DriverInfo()
        {
            this.StartPosNs = -1.0f;
            this.LastPosNs = -1.0f;
            this.ConnectedTimestamp = -1;
        }

        private const double MaxSpeed = 1000; // km/h
        private const double MinSpeed = 5; // km/h

        [DataMember]
        public int ConnectionId { get; set; }
        [DataMember]
        public long ConnectedTimestamp { get; set; }
        [DataMember]
        public long DisconnectedTimestamp { get; set; }
        [DataMember]
        public string DriverGuid { get; set; }
        [DataMember]
        public string DriverName { get; set; }
        [DataMember]
        public string DriverTeam { get; set; } // currently not set
        [DataMember]
        public byte CarId { get; set; }
        [DataMember]
        public string CarModel { get; set; }
        [DataMember]
        public string CarSkin { get; set; }
        [DataMember]
        public ushort BallastKG { get; set; } // currently not set
        [DataMember]
        public uint BestLap { get; set; }
        [DataMember]
        public uint TotalTime { get; set; }
        [DataMember]
        public ushort LapCount { get; set; }
        [DataMember]
        public ushort StartPosition { get; set; } // only set for race session
        [DataMember]
        public ushort Position { get; set; }
        [DataMember]
        public string Gap { get; set; }
        [DataMember]
        public int Incidents { get; set; }
        [DataMember]
        public float Distance { get; set; }
        [DataMember]
        public float TopSpeed { get; set; } // km/h
        [DataMember]
        public float StartPosNs { get; set; }
        [DataMember]
        public float LastPosNs { get; set; }
        [DataMember]
        public bool IsAdmin { get; set; }

        private int lastTime = -1;
        private Vector3f lastPos, lastVel;
        private float lastSplinePos;

        /// <summary>
        /// <see cref="Environment.TickCount"/> of the last position update.
        /// </summary>
        public int LastPositionUpdate
        {
            get
            {
                return lastTime;
            }
        }

        public Vector3f LastPosition
        {
            get
            {
                return lastPos;
            }
        }

        public Vector3f LastVelocity
        {
            get
            {
                return lastVel;
            }
        }

        public float LastSplinePosition
        {
            get
            {
                return lastSplinePos;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.ConnectedTimestamp != -1 && this.DisconnectedTimestamp == -1;
            }
        }

        public void UpdatePosition(Vector3f pos, Vector3f vel, float s)
        {
            if (StartPosNs == -1.0f)
            {
                StartPosNs = s > 0.5f ? s - 1.0f : s;
            }

            float currentSpeed = vel.Length() * 3.6f;
            if (currentSpeed < MaxSpeed && currentSpeed > TopSpeed)
            {
                this.TopSpeed = currentSpeed;
            }

            int currTime = Environment.TickCount;
            if (this.lastTime > 0)
            {
                float d = (pos - lastPos).Length();

                float speed = d / (currTime - this.lastTime) / 1000 * 3.6f;

                if (speed < MaxSpeed && currentSpeed > MinSpeed)
                {
                    this.Distance += d;
                    this.LastPosNs = s;
                }
            }
            this.lastPos = pos;
            this.lastVel = vel;
            this.lastSplinePos = s;
            this.lastTime = currTime;
        }
    }
}