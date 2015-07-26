﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MinoRatingPlugin.minoRatingServer {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="LeaderboardEntry", Namespace="http://schemas.datacontract.org/2004/07/MinoRating.Core.proxy")]
    [System.SerializableAttribute()]
    public partial class LeaderboardEntry : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int CarIdField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string DriverIdField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int LapsDrivenField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int RankField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private uint TimeField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int CarId {
            get {
                return this.CarIdField;
            }
            set {
                if ((this.CarIdField.Equals(value) != true)) {
                    this.CarIdField = value;
                    this.RaisePropertyChanged("CarId");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string DriverId {
            get {
                return this.DriverIdField;
            }
            set {
                if ((object.ReferenceEquals(this.DriverIdField, value) != true)) {
                    this.DriverIdField = value;
                    this.RaisePropertyChanged("DriverId");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int LapsDriven {
            get {
                return this.LapsDrivenField;
            }
            set {
                if ((this.LapsDrivenField.Equals(value) != true)) {
                    this.LapsDrivenField = value;
                    this.RaisePropertyChanged("LapsDriven");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int Rank {
            get {
                return this.RankField;
            }
            set {
                if ((this.RankField.Equals(value) != true)) {
                    this.RankField = value;
                    this.RaisePropertyChanged("Rank");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public uint Time {
            get {
                return this.TimeField;
            }
            set {
                if ((this.TimeField.Equals(value) != true)) {
                    this.TimeField = value;
                    this.RaisePropertyChanged("Time");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="minoRatingServer.ILiveDataDump")]
    public interface ILiveDataDump {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/Tst", ReplyAction="http://tempuri.org/ILiveDataDump/TstResponse")]
        void Tst();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/Tst", ReplyAction="http://tempuri.org/ILiveDataDump/TstResponse")]
        System.Threading.Tasks.Task TstAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/NewSession", ReplyAction="http://tempuri.org/ILiveDataDump/NewSessionResponse")]
        System.Guid NewSession(System.Guid lastId, string servername, string track, int sessionType, int laps, int waittime, int timeofday, int ambient, int road, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/NewSession", ReplyAction="http://tempuri.org/ILiveDataDump/NewSessionResponse")]
        System.Threading.Tasks.Task<System.Guid> NewSessionAsync(System.Guid lastId, string servername, string track, int sessionType, int laps, int waittime, int timeofday, int ambient, int road, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/NewConnection", ReplyAction="http://tempuri.org/ILiveDataDump/NewConnectionResponse")]
        void NewConnection(System.Guid sessionId, int carId, string car, string name, string driverId, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/NewConnection", ReplyAction="http://tempuri.org/ILiveDataDump/NewConnectionResponse")]
        System.Threading.Tasks.Task NewConnectionAsync(System.Guid sessionId, int carId, string car, string name, string driverId, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/ClosedConnection", ReplyAction="http://tempuri.org/ILiveDataDump/ClosedConnectionResponse")]
        void ClosedConnection(System.Guid sessionId, int carId, string car, string name, string driverId, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/ClosedConnection", ReplyAction="http://tempuri.org/ILiveDataDump/ClosedConnectionResponse")]
        System.Threading.Tasks.Task ClosedConnectionAsync(System.Guid sessionId, int carId, string car, string name, string driverId, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/LapCompleted", ReplyAction="http://tempuri.org/ILiveDataDump/LapCompletedResponse")]
        byte[] LapCompleted(System.Guid sessionId, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/LapCompleted", ReplyAction="http://tempuri.org/ILiveDataDump/LapCompletedResponse")]
        System.Threading.Tasks.Task<byte[]> LapCompletedAsync(System.Guid sessionId, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/Collision", ReplyAction="http://tempuri.org/ILiveDataDump/CollisionResponse")]
        byte[] Collision(System.Guid sessionId, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, string token);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/Collision", ReplyAction="http://tempuri.org/ILiveDataDump/CollisionResponse")]
        System.Threading.Tasks.Task<byte[]> CollisionAsync(System.Guid sessionId, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, string token);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ILiveDataDumpChannel : MinoRatingPlugin.minoRatingServer.ILiveDataDump, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class LiveDataDumpClient : System.ServiceModel.ClientBase<MinoRatingPlugin.minoRatingServer.ILiveDataDump>, MinoRatingPlugin.minoRatingServer.ILiveDataDump {
        
        public LiveDataDumpClient() {
        }
        
        public LiveDataDumpClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public LiveDataDumpClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public LiveDataDumpClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public LiveDataDumpClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void Tst() {
            base.Channel.Tst();
        }
        
        public System.Threading.Tasks.Task TstAsync() {
            return base.Channel.TstAsync();
        }
        
        public System.Guid NewSession(System.Guid lastId, string servername, string track, int sessionType, int laps, int waittime, int timeofday, int ambient, int road, string token) {
            return base.Channel.NewSession(lastId, servername, track, sessionType, laps, waittime, timeofday, ambient, road, token);
        }
        
        public System.Threading.Tasks.Task<System.Guid> NewSessionAsync(System.Guid lastId, string servername, string track, int sessionType, int laps, int waittime, int timeofday, int ambient, int road, string token) {
            return base.Channel.NewSessionAsync(lastId, servername, track, sessionType, laps, waittime, timeofday, ambient, road, token);
        }
        
        public void NewConnection(System.Guid sessionId, int carId, string car, string name, string driverId, string token) {
            base.Channel.NewConnection(sessionId, carId, car, name, driverId, token);
        }
        
        public System.Threading.Tasks.Task NewConnectionAsync(System.Guid sessionId, int carId, string car, string name, string driverId, string token) {
            return base.Channel.NewConnectionAsync(sessionId, carId, car, name, driverId, token);
        }
        
        public void ClosedConnection(System.Guid sessionId, int carId, string car, string name, string driverId, string token) {
            base.Channel.ClosedConnection(sessionId, carId, car, name, driverId, token);
        }
        
        public System.Threading.Tasks.Task ClosedConnectionAsync(System.Guid sessionId, int carId, string car, string name, string driverId, string token) {
            return base.Channel.ClosedConnectionAsync(sessionId, carId, car, name, driverId, token);
        }
        
        public byte[] LapCompleted(System.Guid sessionId, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard, string token) {
            return base.Channel.LapCompleted(sessionId, car, driver, laptime, cuts, grip, leaderboard, token);
        }
        
        public System.Threading.Tasks.Task<byte[]> LapCompletedAsync(System.Guid sessionId, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard, string token) {
            return base.Channel.LapCompletedAsync(sessionId, car, driver, laptime, cuts, grip, leaderboard, token);
        }
        
        public byte[] Collision(System.Guid sessionId, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, string token) {
            return base.Channel.Collision(sessionId, car, otherCar, speed, splinepos, relativeX, relativeZ, x, z, token);
        }
        
        public System.Threading.Tasks.Task<byte[]> CollisionAsync(System.Guid sessionId, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, string token) {
            return base.Channel.CollisionAsync(sessionId, car, otherCar, speed, splinepos, relativeX, relativeZ, x, z, token);
        }
    }
}