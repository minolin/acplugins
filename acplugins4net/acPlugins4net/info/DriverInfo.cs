using acPlugins4net.helpers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace acPlugins4net.info
{
    [DataContract]
    public class DriverInfo
    {
        #region MsgCarUpdate cache  -  No idea how we use this, and if it's cool at all

        /// <summary>
        /// Defines how many MsgCarUpdates are cached (for a look in the past)
        /// </summary>
        [IgnoreDataMember]
        public static int MsgCarUpdateCacheSize { get; set; } = 0;
        [IgnoreDataMember]
        private LinkedList<messages.MsgCarUpdate> _carUpdateCache = new LinkedList<messages.MsgCarUpdate>();
        public LinkedListNode<messages.MsgCarUpdate> LastCarUpdate { get { return _carUpdateCache.Last; } }

        #endregion

        private const double MaxSpeed = 1000; // km/h
        private const double MinSpeed = 5; // km/h

        [DataMember]
        public int ConnectionId { get; set; }
        [DataMember]
        public long ConnectedTimestamp { get; set; } = -1;
        [DataMember]
        public long DisconnectedTimestamp { get; set; } = -1;
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
        public ushort Position { get; set; } // rename to e.g. Grid- or RacePosition? Easily mixed up with the Vector3 Positions
        [DataMember]
        public string Gap { get; set; }
        [DataMember]
        public int Incidents { get; set; }
        [DataMember]
        public float Distance { get; set; }
        [IgnoreDataMember]
        public float CurrentSpeed { get; set; } // km/h
        [IgnoreDataMember]
        public float CurrentAcceleration { get; set; } // km/h
        [DataMember]
        public float TopSpeed { get; set; } // km/h
        [DataMember]
        public float StartSplinePos { get; set; } = -1f;
        [DataMember]
        public float LastSplinePos { get; set; } = -1f;
        [DataMember]
        public bool IsAdmin { get; set; }

        public bool IsConnected
        {
            get
            {
                return this.ConnectedTimestamp != -1 && this.DisconnectedTimestamp == -1;
            }
        }

        private int lastTime = -1;
        private Vector3f lastPos, lastVel;
        private float lastSplinePos;

        private float lapDistance;
        private float lastDistanceTraveled;
        private float lapStartSplinePos = -1f;

        #region getter for some 'realtime' positional info
        public float LapDistance
        {
            get
            {
                return this.lapDistance;
            }
        }

        [IgnoreDataMember]
        public float LastDistanceTraveled
        {
            get
            {
                return this.lastDistanceTraveled;
            }
        }

        public float LapStartSplinePos
        {
            get
            {
                return this.lapStartSplinePos;
            }
        }

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

        /// <summary>
        /// Expresses the distance in meters to the nearest car, either in front or back, ignoring positions.
        /// Zero if there is new other (moving) car
        /// </summary>
        [IgnoreDataMember]
        public float CurrentDistanceToClosestCar { get; set; }

        #endregion

        // That cache<MsgCarUpdate> should be replaced by a cache<CarUpdateThing> that also stores
        // the timestamp, otherwise calculations are always squishy (and e.g. dependent on the interval)
        public void UpdatePosition(messages.MsgCarUpdate msg, int realtimeUpdateInterval)
        {
            UpdatePosition(msg.WorldPosition, msg.Velocity, msg.NormalizedSplinePosition, realtimeUpdateInterval);
            if (MsgCarUpdateCacheSize > 0)
            {
                var node = _carUpdateCache.AddLast(msg);
                if (_carUpdateCache.Count > MsgCarUpdateCacheSize)
                    _carUpdateCache.RemoveFirst();

                if (_carUpdateCache.Count > 1)
                {
                    // We could easily do car-specifc stuff here, e.g. calculate the distance driven between the intervals,
                    // or a python-app like delta - maybe even a loss of control
                }
            }
        }

        public void UpdatePosition(Vector3f pos, Vector3f vel, float s, int realtimeUpdateInterval)
        {
            if (this.StartSplinePos == -1.0f)
            {
                this.StartSplinePos = s > 0.5f ? s - 1.0f : s;
            }

            if (this.lapStartSplinePos == -1.0f)
            {
                this.lapStartSplinePos = s > 0.5f ? s - 1.0f : s;
            }

            // Determine the current speed in KpH (only valid if the update interval is 1s)
            CurrentSpeed = vel.Length() * 3.6f;
            if (CurrentSpeed < MaxSpeed && CurrentSpeed > TopSpeed)
            {
                this.TopSpeed = CurrentSpeed;
            }

            // Determine the current acceleration in Kph/s (only valid if the update interval is 1s)
            var lastSpeed = lastVel.Length() * 3.6f;
            CurrentAcceleration = CurrentSpeed - lastSpeed;

            int currTime = Environment.TickCount & Int32.MaxValue; // see https://msdn.microsoft.com/de-de/library/system.environment.tickcount%28v=vs.110%29.aspx
            int elapsedSinceLastUpdate = currTime - this.lastTime;
            if (this.lastTime > 0 && elapsedSinceLastUpdate > 0 && elapsedSinceLastUpdate < 3 * realtimeUpdateInterval)
            {
                float d = (pos - lastPos).Length();
                float speed = d / elapsedSinceLastUpdate / 1000 * 3.6f;

                //if (! (speed > 30 && vel.X == 0 && vel.Z == 0)) // If the car was moving according (speed), but the vel is 2d-zero now, the car has either good brakes or is warped

                // If the computed average speed since last update is not much bigger than the maximum of last vel and the current vel then no warp detected.
                // in worst case warps that occur from near the pits (~50m) are not detected.
                if (speed - Math.Max(CurrentSpeed, lastSpeed) < 180 * elapsedSinceLastUpdate / 1000)
                {
                    // no warp detected
                    this.lapDistance += d;
                    this.Distance += d;
                    this.lastDistanceTraveled = d;

                    if (CurrentSpeed > MinSpeed)
                    {
                        // don't update LastSplinePos if car is moving very slowly (was send to box?)
                        this.LastSplinePos = s;
                    }
                }
                else
                {
                    //Console.WriteLine("Car " + CarId + " warped with speed " + speed);
                    // probably warped to box
                    this.lapDistance = 0;
                    this.lapStartSplinePos = s > 0.5f ? s - 1.0f : s;
                }
            }
            this.lastPos = pos;
            this.lastVel = vel;
            this.lastSplinePos = s;
            this.lastTime = currTime;
        }

        public float OnLapCompleted()
        {
            float lastSplinePos = this.LastSplinePos;
            if (lastSplinePos < 0.5)
            {
                lastSplinePos += 1f;
            }

            float splinePosDiff = lastSplinePos - this.lapStartSplinePos;
            float lapLength = this.lapDistance / splinePosDiff;

            this.lapStartSplinePos = lastSplinePos - 1f;
            this.lapDistance = 0f;
            this.lastSplinePos = 0.0f;

            return lapLength;
        }
    }
}