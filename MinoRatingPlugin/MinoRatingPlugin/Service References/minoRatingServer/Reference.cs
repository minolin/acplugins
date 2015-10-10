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
    [System.Runtime.Serialization.DataContractAttribute(Name="PluginReaction", Namespace="http://schemas.datacontract.org/2004/07/MinoRating.Core.proxy")]
    [System.SerializableAttribute()]
    public partial class PluginReaction : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private byte CarIdField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int DelayField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string NameField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private MinoRatingPlugin.minoRatingServer.PluginReaction.ReactionType ReactionField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string SteamIdField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string TextField;
        
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
        public byte CarId {
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
        public int Delay {
            get {
                return this.DelayField;
            }
            set {
                if ((this.DelayField.Equals(value) != true)) {
                    this.DelayField = value;
                    this.RaisePropertyChanged("Delay");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Name {
            get {
                return this.NameField;
            }
            set {
                if ((object.ReferenceEquals(this.NameField, value) != true)) {
                    this.NameField = value;
                    this.RaisePropertyChanged("Name");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public MinoRatingPlugin.minoRatingServer.PluginReaction.ReactionType Reaction {
            get {
                return this.ReactionField;
            }
            set {
                if ((this.ReactionField.Equals(value) != true)) {
                    this.ReactionField = value;
                    this.RaisePropertyChanged("Reaction");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string SteamId {
            get {
                return this.SteamIdField;
            }
            set {
                if ((object.ReferenceEquals(this.SteamIdField, value) != true)) {
                    this.SteamIdField = value;
                    this.RaisePropertyChanged("SteamId");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Text {
            get {
                return this.TextField;
            }
            set {
                if ((object.ReferenceEquals(this.TextField, value) != true)) {
                    this.TextField = value;
                    this.RaisePropertyChanged("Text");
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
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
        [System.Runtime.Serialization.DataContractAttribute(Name="PluginReaction.ReactionType", Namespace="http://schemas.datacontract.org/2004/07/MinoRating.Core.proxy")]
        public enum ReactionType : int {
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            None = 0,
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            Whisper = 1,
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            Broadcast = 2,
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            Ballast = 3,
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            Pit = 4,
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            Kick = 5,
            
            [System.Runtime.Serialization.EnumMemberAttribute()]
            Ban = 6,
        }
    }
    
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
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="MRDistanceHelper", Namespace="http://schemas.datacontract.org/2004/07/MinoRating.Core.proxy")]
    [System.SerializableAttribute()]
    public partial class MRDistanceHelper : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float MetersAttackRangeField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float MetersBlueflaggingField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float MetersCombatRangeField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float MetersDrivenField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int OvertakesField;
        
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
        public float MetersAttackRange {
            get {
                return this.MetersAttackRangeField;
            }
            set {
                if ((this.MetersAttackRangeField.Equals(value) != true)) {
                    this.MetersAttackRangeField = value;
                    this.RaisePropertyChanged("MetersAttackRange");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public float MetersBlueflagging {
            get {
                return this.MetersBlueflaggingField;
            }
            set {
                if ((this.MetersBlueflaggingField.Equals(value) != true)) {
                    this.MetersBlueflaggingField = value;
                    this.RaisePropertyChanged("MetersBlueflagging");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public float MetersCombatRange {
            get {
                return this.MetersCombatRangeField;
            }
            set {
                if ((this.MetersCombatRangeField.Equals(value) != true)) {
                    this.MetersCombatRangeField = value;
                    this.RaisePropertyChanged("MetersCombatRange");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public float MetersDriven {
            get {
                return this.MetersDrivenField;
            }
            set {
                if ((this.MetersDrivenField.Equals(value) != true)) {
                    this.MetersDrivenField = value;
                    this.RaisePropertyChanged("MetersDriven");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int Overtakes {
            get {
                return this.OvertakesField;
            }
            set {
                if ((this.OvertakesField.Equals(value) != true)) {
                    this.OvertakesField = value;
                    this.RaisePropertyChanged("Overtakes");
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
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="CarUpdateHistory", Namespace="http://schemas.datacontract.org/2004/07/MinoRating.Core.proxy")]
    [System.SerializableAttribute()]
    public partial class CarUpdateHistory : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private System.DateTime CreatedField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private ushort EngineRPMField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private byte GearField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float NormalizedSplinePositionField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float[] VelocityField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private float[] WorldPositionField;
        
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
        public System.DateTime Created {
            get {
                return this.CreatedField;
            }
            set {
                if ((this.CreatedField.Equals(value) != true)) {
                    this.CreatedField = value;
                    this.RaisePropertyChanged("Created");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public ushort EngineRPM {
            get {
                return this.EngineRPMField;
            }
            set {
                if ((this.EngineRPMField.Equals(value) != true)) {
                    this.EngineRPMField = value;
                    this.RaisePropertyChanged("EngineRPM");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public byte Gear {
            get {
                return this.GearField;
            }
            set {
                if ((this.GearField.Equals(value) != true)) {
                    this.GearField = value;
                    this.RaisePropertyChanged("Gear");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public float NormalizedSplinePosition {
            get {
                return this.NormalizedSplinePositionField;
            }
            set {
                if ((this.NormalizedSplinePositionField.Equals(value) != true)) {
                    this.NormalizedSplinePositionField = value;
                    this.RaisePropertyChanged("NormalizedSplinePosition");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public float[] Velocity {
            get {
                return this.VelocityField;
            }
            set {
                if ((object.ReferenceEquals(this.VelocityField, value) != true)) {
                    this.VelocityField = value;
                    this.RaisePropertyChanged("Velocity");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public float[] WorldPosition {
            get {
                return this.WorldPositionField;
            }
            set {
                if ((object.ReferenceEquals(this.WorldPositionField, value) != true)) {
                    this.WorldPositionField = value;
                    this.RaisePropertyChanged("WorldPosition");
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
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/NewSession", ReplyAction="http://tempuri.org/ILiveDataDump/NewSessionResponse")]
        System.Guid NewSession(
                    System.Guid lastId, 
                    string servername, 
                    string track, 
                    int sessionType, 
                    int laps, 
                    int waittime, 
                    int sessionDurationMinutes, 
                    int ambient, 
                    int road, 
                    int elapsedMs, 
                    string token, 
                    byte[] fingerprint, 
                    System.Version pluginVersion, 
                    int sessionCollisionsToKick, 
                    int sessionMassAccidentsToKick, 
                    int serverKickMode);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/NewSession", ReplyAction="http://tempuri.org/ILiveDataDump/NewSessionResponse")]
        System.Threading.Tasks.Task<System.Guid> NewSessionAsync(
                    System.Guid lastId, 
                    string servername, 
                    string track, 
                    int sessionType, 
                    int laps, 
                    int waittime, 
                    int sessionDurationMinutes, 
                    int ambient, 
                    int road, 
                    int elapsedMs, 
                    string token, 
                    byte[] fingerprint, 
                    System.Version pluginVersion, 
                    int sessionCollisionsToKick, 
                    int sessionMassAccidentsToKick, 
                    int serverKickMode);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/EndSession", ReplyAction="http://tempuri.org/ILiveDataDump/EndSessionResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] EndSession(System.Guid lastId);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/EndSession", ReplyAction="http://tempuri.org/ILiveDataDump/EndSessionResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> EndSessionAsync(System.Guid lastId);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/LapCompleted", ReplyAction="http://tempuri.org/ILiveDataDump/LapCompletedResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] LapCompleted(System.Guid sessionId, System.DateTime created, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/LapCompleted", ReplyAction="http://tempuri.org/ILiveDataDump/LapCompletedResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> LapCompletedAsync(System.Guid sessionId, System.DateTime created, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/DistanceDriven", ReplyAction="http://tempuri.org/ILiveDataDump/DistanceDrivenResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] DistanceDriven(System.Guid sessionId, int car, [System.ServiceModel.MessageParameterAttribute(Name="distanceDriven")] MinoRatingPlugin.minoRatingServer.MRDistanceHelper distanceDriven1);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/DistanceDriven", ReplyAction="http://tempuri.org/ILiveDataDump/DistanceDrivenResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> DistanceDrivenAsync(System.Guid sessionId, int car, MinoRatingPlugin.minoRatingServer.MRDistanceHelper distanceDriven);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/Collision", ReplyAction="http://tempuri.org/ILiveDataDump/CollisionResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] Collision(System.Guid sessionId, System.DateTime created, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyCar, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyOtherCar, int bagId);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/Collision", ReplyAction="http://tempuri.org/ILiveDataDump/CollisionResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> CollisionAsync(System.Guid sessionId, System.DateTime created, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyCar, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyOtherCar, int bagId);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/CollisionTreeEnded", ReplyAction="http://tempuri.org/ILiveDataDump/CollisionTreeEndedResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] CollisionTreeEnded(System.Guid sessionId, int car, int otherCar, int count, System.DateTime started, System.DateTime ended);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/CollisionTreeEnded", ReplyAction="http://tempuri.org/ILiveDataDump/CollisionTreeEndedResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> CollisionTreeEndedAsync(System.Guid sessionId, int car, int otherCar, int count, System.DateTime started, System.DateTime ended);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RandomCarInfo", ReplyAction="http://tempuri.org/ILiveDataDump/RandomCarInfoResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] RandomCarInfo(System.Guid sessionId, int carId, string car, string name, string driverId, bool isConnected, int sessionTime);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RandomCarInfo", ReplyAction="http://tempuri.org/ILiveDataDump/RandomCarInfoResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RandomCarInfoAsync(System.Guid sessionId, int carId, string car, string name, string driverId, bool isConnected, int sessionTime);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/GetVersion", ReplyAction="http://tempuri.org/ILiveDataDump/GetVersionResponse")]
        System.Version GetVersion();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/GetVersion", ReplyAction="http://tempuri.org/ILiveDataDump/GetVersionResponse")]
        System.Threading.Tasks.Task<System.Version> GetVersionAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RequestDriverRating", ReplyAction="http://tempuri.org/ILiveDataDump/RequestDriverRatingResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] RequestDriverRating(System.Guid sessionId, int car);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RequestDriverRating", ReplyAction="http://tempuri.org/ILiveDataDump/RequestDriverRatingResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RequestDriverRatingAsync(System.Guid sessionId, int car);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RequestMRCommand", ReplyAction="http://tempuri.org/ILiveDataDump/RequestMRCommandResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] RequestMRCommand(System.Guid sessionId, int car, string[] parameters);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RequestMRCommand", ReplyAction="http://tempuri.org/ILiveDataDump/RequestMRCommandResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RequestMRCommandAsync(System.Guid sessionId, int car, string[] parameters);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RequestDriverLoaded", ReplyAction="http://tempuri.org/ILiveDataDump/RequestDriverLoadedResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] RequestDriverLoaded(System.Guid sessionId, int car);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/RequestDriverLoaded", ReplyAction="http://tempuri.org/ILiveDataDump/RequestDriverLoadedResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RequestDriverLoadedAsync(System.Guid sessionId, int car);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/GetPendingActions", ReplyAction="http://tempuri.org/ILiveDataDump/GetPendingActionsResponse")]
        MinoRatingPlugin.minoRatingServer.PluginReaction[] GetPendingActions(System.Guid sessionId);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILiveDataDump/GetPendingActions", ReplyAction="http://tempuri.org/ILiveDataDump/GetPendingActionsResponse")]
        System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> GetPendingActionsAsync(System.Guid sessionId);
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
        
        public System.Guid NewSession(
                    System.Guid lastId, 
                    string servername, 
                    string track, 
                    int sessionType, 
                    int laps, 
                    int waittime, 
                    int sessionDurationMinutes, 
                    int ambient, 
                    int road, 
                    int elapsedMs, 
                    string token, 
                    byte[] fingerprint, 
                    System.Version pluginVersion, 
                    int sessionCollisionsToKick, 
                    int sessionMassAccidentsToKick, 
                    int serverKickMode) {
            return base.Channel.NewSession(lastId, servername, track, sessionType, laps, waittime, sessionDurationMinutes, ambient, road, elapsedMs, token, fingerprint, pluginVersion, sessionCollisionsToKick, sessionMassAccidentsToKick, serverKickMode);
        }
        
        public System.Threading.Tasks.Task<System.Guid> NewSessionAsync(
                    System.Guid lastId, 
                    string servername, 
                    string track, 
                    int sessionType, 
                    int laps, 
                    int waittime, 
                    int sessionDurationMinutes, 
                    int ambient, 
                    int road, 
                    int elapsedMs, 
                    string token, 
                    byte[] fingerprint, 
                    System.Version pluginVersion, 
                    int sessionCollisionsToKick, 
                    int sessionMassAccidentsToKick, 
                    int serverKickMode) {
            return base.Channel.NewSessionAsync(lastId, servername, track, sessionType, laps, waittime, sessionDurationMinutes, ambient, road, elapsedMs, token, fingerprint, pluginVersion, sessionCollisionsToKick, sessionMassAccidentsToKick, serverKickMode);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] EndSession(System.Guid lastId) {
            return base.Channel.EndSession(lastId);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> EndSessionAsync(System.Guid lastId) {
            return base.Channel.EndSessionAsync(lastId);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] LapCompleted(System.Guid sessionId, System.DateTime created, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard) {
            return base.Channel.LapCompleted(sessionId, created, car, driver, laptime, cuts, grip, leaderboard);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> LapCompletedAsync(System.Guid sessionId, System.DateTime created, int car, string driver, uint laptime, int cuts, float grip, MinoRatingPlugin.minoRatingServer.LeaderboardEntry[] leaderboard) {
            return base.Channel.LapCompletedAsync(sessionId, created, car, driver, laptime, cuts, grip, leaderboard);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] DistanceDriven(System.Guid sessionId, int car, MinoRatingPlugin.minoRatingServer.MRDistanceHelper distanceDriven1) {
            return base.Channel.DistanceDriven(sessionId, car, distanceDriven1);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> DistanceDrivenAsync(System.Guid sessionId, int car, MinoRatingPlugin.minoRatingServer.MRDistanceHelper distanceDriven) {
            return base.Channel.DistanceDrivenAsync(sessionId, car, distanceDriven);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] Collision(System.Guid sessionId, System.DateTime created, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyCar, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyOtherCar, int bagId) {
            return base.Channel.Collision(sessionId, created, car, otherCar, speed, splinepos, relativeX, relativeZ, x, z, historyCar, historyOtherCar, bagId);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> CollisionAsync(System.Guid sessionId, System.DateTime created, int car, int otherCar, float speed, float splinepos, float relativeX, float relativeZ, float x, float z, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyCar, MinoRatingPlugin.minoRatingServer.CarUpdateHistory[] historyOtherCar, int bagId) {
            return base.Channel.CollisionAsync(sessionId, created, car, otherCar, speed, splinepos, relativeX, relativeZ, x, z, historyCar, historyOtherCar, bagId);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] CollisionTreeEnded(System.Guid sessionId, int car, int otherCar, int count, System.DateTime started, System.DateTime ended) {
            return base.Channel.CollisionTreeEnded(sessionId, car, otherCar, count, started, ended);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> CollisionTreeEndedAsync(System.Guid sessionId, int car, int otherCar, int count, System.DateTime started, System.DateTime ended) {
            return base.Channel.CollisionTreeEndedAsync(sessionId, car, otherCar, count, started, ended);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] RandomCarInfo(System.Guid sessionId, int carId, string car, string name, string driverId, bool isConnected, int sessionTime) {
            return base.Channel.RandomCarInfo(sessionId, carId, car, name, driverId, isConnected, sessionTime);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RandomCarInfoAsync(System.Guid sessionId, int carId, string car, string name, string driverId, bool isConnected, int sessionTime) {
            return base.Channel.RandomCarInfoAsync(sessionId, carId, car, name, driverId, isConnected, sessionTime);
        }
        
        public System.Version GetVersion() {
            return base.Channel.GetVersion();
        }
        
        public System.Threading.Tasks.Task<System.Version> GetVersionAsync() {
            return base.Channel.GetVersionAsync();
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] RequestDriverRating(System.Guid sessionId, int car) {
            return base.Channel.RequestDriverRating(sessionId, car);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RequestDriverRatingAsync(System.Guid sessionId, int car) {
            return base.Channel.RequestDriverRatingAsync(sessionId, car);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] RequestMRCommand(System.Guid sessionId, int car, string[] parameters) {
            return base.Channel.RequestMRCommand(sessionId, car, parameters);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RequestMRCommandAsync(System.Guid sessionId, int car, string[] parameters) {
            return base.Channel.RequestMRCommandAsync(sessionId, car, parameters);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] RequestDriverLoaded(System.Guid sessionId, int car) {
            return base.Channel.RequestDriverLoaded(sessionId, car);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> RequestDriverLoadedAsync(System.Guid sessionId, int car) {
            return base.Channel.RequestDriverLoadedAsync(sessionId, car);
        }
        
        public MinoRatingPlugin.minoRatingServer.PluginReaction[] GetPendingActions(System.Guid sessionId) {
            return base.Channel.GetPendingActions(sessionId);
        }
        
        public System.Threading.Tasks.Task<MinoRatingPlugin.minoRatingServer.PluginReaction[]> GetPendingActionsAsync(System.Guid sessionId) {
            return base.Channel.GetPendingActionsAsync(sessionId);
        }
    }
}
