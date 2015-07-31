from ac_server_helpers import *

__all__ = ["NewSession","CollisionEnv","CollisionCar","CarInfo","CarUpdate","NewConnection","ConnectionClosed","LapCompleted"]

ACSP_NEW_SESSION = 50
ACSP_NEW_CONNECTION = 51
ACSP_CONNECTION_CLOSED = 52
ACSP_CAR_UPDATE = 53
ACSP_CAR_INFO = 54 # Sent as response to ACSP_GET_CAR_INFO command
ACSP_LAP_COMPLETED = 73

ACSP_CLIENT_EVENT = 130

ACSP_CE_COLLISION_WITH_CAR = 10
ACSP_CE_COLLISION_WITH_ENV = 11

ACSP_REALTIMEPOS_INTERVAL = 200
ACSP_GET_CAR_INFO = 201
ACSP_SEND_CHAT = 202 # Sends chat to one car
ACSP_BROADCAST_CHAT = 203 # Sends chat to everybody 
        
class NewSession(GenericPacket):
    packetId = ACSP_NEW_SESSION
    _content = (
        ('name', Ascii),
        ('sessionType', Uint8),
        ('timeOfDay', Uint16),
        ('laps', Uint16),
        ('waittime', Uint16),
        ('ambientTemp', Uint8),
        ('roadTemp', Uint8),
        ('wheather', Ascii)
    )       

class CollisionEnv(GenericPacket):
    packetId = ACSP_CLIENT_EVENT
    _content = (
        ('carId', Uint8),
        ('impactSpeed', Float),
        ('worldPos', Vector3f),
        ('relPos', Vector3f),
    )
    
class CollisionCar(GenericPacket):
    packetId = ACSP_CLIENT_EVENT
    _content = (
        ('car1_id', Uint8),
        ('car2_id', Uint8),
        ('impactSpeed', Float),
        ('worldPos', Vector3f),
        ('relPos', Vector3f),
    )
    
class ClientEvent:
    packetId = ACSP_CLIENT_EVENT
    def from_buffer(self, buffer, idx):
        evtype,idx = Uint8.get(buffer, idx)
        if evtype == ACSP_CE_COLLISION_WITH_CAR:
            return CollisionCar().from_buffer(buffer, idx)
        elif evtype == ACSP_CE_COLLISION_WITH_ENV:
            return CollisionEnv().from_buffer(buffer, idx)

class CarInfo(GenericPacket):
    packetId = ACSP_CAR_INFO
    _content = (
        ('carId', Uint8),
        ('isConnected', Bool),
        ('carModel', UTF32),
        ('carSkin', UTF32),
        ('driverName', UTF32),
        ('driverTeam', UTF32),
        ('driverGuid', UTF32),
    )
    
class CarUpdate(GenericPacket):
    packetId = ACSP_CAR_UPDATE
    _content = (
        ('carId', Uint8),
        ('worldPos', Vector3f),
        ('velocity', Vector3f),
        ('gear', Uint8),
        ('engineRPM', Uint16),
        ('normalizedSplinePos', Float),
    )

class NewConnection(GenericPacket):
    packetId = ACSP_NEW_CONNECTION
    _content = (
        ('driverName', UTF32),
        ('driverGuid', UTF32),
        ('carId', Uint8),
        ('carModel', Ascii), # this is different type than CarInfo
        ('carSkin', Ascii),  # this is different type than CarInfo
    )
    
class ConnectionClosed(GenericPacket):
    packetId = ACSP_CONNECTION_CLOSED
    _content = (
        ('driverName', UTF32),
        ('driverGuid', UTF32),
        ('carId', Uint8),
        ('carModel', Ascii), # this is different type than CarInfo
        ('carSkin', Ascii),  # this is different type than CarInfo
    )
    
class LeaderboardEntry(GenericPacket):
    packetId = ACSP_LAP_COMPLETED
    _content = (
        ('carId', Uint8),
        ('lapTime', Uint32),
        ('laps', Uint8),
    )

Leaderboard = GenericArrayParser('B', 6,
    lambda x: tuple(LeaderboardEntry().from_buffer(x[(i*6):((i+1)*6)], 0)[1] for i in range(len(x)//6)),
    None,
)
class LapCompleted(GenericPacket):
    packetId = ACSP_LAP_COMPLETED
    _content = (
        ('carId', Uint8),
        ('lapTime', Uint32),
        ('cuts', Uint8),
        ('leaderboard', Leaderboard),
        ('gripLevel', Float),
    )

class GetCarInfo(GenericPacket):
    packetId = ACSP_GET_CAR_INFO
    _content = (
        ('carId', Uint8),
    )
    
class EnableRealtimeReport(GenericPacket):
    packetId = ACSP_REALTIMEPOS_INTERVAL
    _content = (
        ('intervalMS', Uint16),
    )
    
class SendChat(GenericPacket):
    packetId = ACSP_SEND_CHAT
    _content = (
        ('carId', Uint8),
        ('message', UTF32),
    )
    
class BroadcastChat(GenericPacket):
    packetId = ACSP_BROADCAST_CHAT
    _content = (
        ('message', UTF32),
    )
    
eventMap = {
}
for e in [NewSession, 
          ClientEvent, 
          CarInfo, 
          CarUpdate, 
          NewConnection, 
          ConnectionClosed, 
          LapCompleted,
          EnableRealtimeReport]:
    eventMap[e.packetId] = e

def parse(buffer):
    eID,idx = Uint8.get(buffer,0)
    if eID in eventMap:
        r = eventMap[eID]()
        idx,r = r.from_buffer(buffer, idx)
        return r
    return None
    