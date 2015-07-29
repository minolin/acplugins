import socket, time
import ac_server_protocol

class ACServerPlugin:

    # create a new server plugin instance, given 
    # - rcvPort  : used to receive the messages from the AC server
    # - sendPort : used to send requests to the AC server
    # - callbacks: a list of callables (or a single callable) called 
    #              whenever a new message is received from the server
    #              the function receives one argument with an instance
    #              of the data sent by the server. 
    #              (see ac_server_protocol for details)
    def __init__(self, rcvPort, sendPort, callbacks, serverIP="127.0.0.1"):
        try:
            _ = iter(callbacks)
        except TypeError:
            callbacks = [callbacks]
        self.callbacks = callbacks 
        
        self.sendSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sendSocket.connect( (serverIP, sendPort) )
        
        self.rcvSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.rcvSocket.bind( (serverIP, rcvPort) )
        
    # call this function to process server packets for a given
    # timespan (timeout) or forever, if timeout is set to None
    def processServerPackets(self, timeout=None):
        t = time.time()
        while 1:
            try:
                self.rcvSocket.settimeout(timeout)
                d = self.rcvSocket.recvfrom(2048)
            except socket.timeout:
                break
            data = d[0]
            addr = d[1]
            r = ac_server_protocol.parse(data)
            for c in self.callbacks:
                c(r)
            if not timeout is None:
                timeout = timeout - (time.time() - t)
                if timeout <= 0.0:
                    break
   
    # request the car info packet from the server
    def getCarInfo(self, carId):
        p = ac_server_protocol.GetCarInfo(carId=carId)
        self.sendSocket.send(p.to_buffer())
    
    # enable the realtime report with a given interval
    def enableRealtimeReport(self, intervalMS):
        p = ac_server_protocol.EnableRealtimeReport(intervalMS=intervalMS)
        self.sendSocket.send(p.to_buffer())
        
    # send chat message to a specific car
    def sendChat(self, carId, message):
        p = ac_server_protocol.SendChat(carId=carId, message=message)
        self.sendSocket.send(p.to_buffer())
        
    # broadcast chat message to all cars
    def broadcastChat(self, message):
        p = ac_server_protocol.BroadcastChat(message=message)
        self.sendSocket.send(p.to_buffer())
    
if __name__ == "__main__":
    # example usage
    import sys, time
    s = ACServerPlugin(int(sys.argv[1]), int(sys.argv[2]), lambda x: print(x))
    s.getCarInfo(7)
    s.processServerPackets(5)
    s.enableRealtimeReport(1000)
    s.processServerPackets(5)
    s.sendChat(7, "Hello 7")
    s.processServerPackets(5)
    s.broadcastChat("Hello all")
    s.processServerPackets(float(sys.argv[3]) if len(sys.argv) >= 4 else None)
    