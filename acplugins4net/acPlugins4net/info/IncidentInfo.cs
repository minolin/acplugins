using acPlugins4net.helpers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace acPlugins4net.info
{
    [DataContract]
    public class IncidentInfo
    {
        [DataMember]
        public byte Type { get; set; }
        [DataMember]
        public long Timestamp { get; set; }
        [DataMember]
        public int ConnectionId1 { get; set; }
        [DataMember]
        public int ConnectionId2 { get; set; }
        [DataMember]
        public float ImpactSpeed { get; set; }
        [DataMember]
        public Vector3f WorldPosition { get; set; }
        [DataMember]
        public Vector3f RelPosition { get; set; }
    }
}