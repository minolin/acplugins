from threading import Thread
import socket, time, select
from . import ac_server_protocol

class ACServerPlugin:

    def __init__(self, rcvPort, sendPort, callbacks, proxyRcvPort=None, proxySendPort=None, serverIP="127.0.0.1"):
        """
        create a new server plugin instance, given 
            - rcvPort      : used to receive the messages from the AC server
            - sendPort     : used to send requests to the AC server
            - callbacks    : a list of callables (or a single callable) called 
                             whenever a new message is received from the server
                             the function receives one argument with an instance
                             of the data sent by the server. 
                             (see ac_server_protocol for details)
            - proxyRcvPort : (optional) a port where the received messages are 
                             forwarded to. With this, a simple chaining of 
                             plugins is possible.
            - proxySendPort: (optional) messages sent to this port will be 
                             forwarded to the AC server. With this, a simple 
                             chaining of plugins is possible.
        """
        try:
            _ = iter(callbacks)
        except TypeError:
            callbacks = [callbacks]
        self.callbacks = callbacks 
        
        self.host = serverIP
        self.sendPort = sendPort
        self.acSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.acSocket.bind( (serverIP, rcvPort) )
        # set up a 0.5s pulse, need this to be able to Ctrl-C the python apps
        self.acSocket.settimeout(0.5)
        
        if not proxyRcvPort is None and not proxySendPort is None:
            self.proxyRcvPort = proxyRcvPort
            self.proxySocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.proxySocket.bind( (serverIP, proxySendPort) )
            # set up a 0.5s pulse, need this to be able to Ctrl-C the python apps
            self.proxySocket.settimeout(0.5)
            self.proxySocket = Thread(target=self._performProxy)
            self.proxySocket.daemon=True
            self.proxySocket.start()
        else:
            self.proxySocket = None
            
        self.realtimeReport = None
                    
    def processServerPackets(self, timeout=None):
        """
        call this function to process server packets for a given
        timespan (timeout) or forever, if timeout is set to None
        """
        t = time.time()
        while 1:
            try:
                data, addr = self.acSocket.recvfrom(2048)
                if not self.proxySocket is None:
                    self.proxySocket.sendto(data, ("127.0.0.1", self.proxyRcvPort))
                r = ac_server_protocol.parse(data)
                for c in self.callbacks:
                    c(r)
            except socket.timeout:
                pass
            if not timeout is None:
                if time.time()-t > timeout:
                    break
   
    def getCarInfo(self, carId):
        """
        request the car info packet from the server
        """
        p = ac_server_protocol.GetCarInfo(carId=carId)
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
    
    def enableRealtimeReport(self, intervalMS):
        """
        enable the realtime report with a given interval
        """
        if self.realtimeReport is None or intervalMS < self.realtimeReport:
            p = ac_server_protocol.EnableRealtimeReport(intervalMS=intervalMS)
            self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
        
    def sendChat(self, carId, message):
        """
        send chat message to a specific car
        """
        p = ac_server_protocol.SendChat(carId=carId, message=message)
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
        
    def broadcastChat(self, message):
        """
        broadcast chat message to all cars
        """
        p = ac_server_protocol.BroadcastChat(message=message)
        d = p.to_buffer()
        self.acSocket.sendto(d, (self.host,self.sendPort))
        
    def _performProxy(self):
        while 1:
            try:
                data, addr = self.proxySocket.recvfrom(2048)
                if addr == "127.0.0.1":
                    r = ac_server_protocol.parse(data)
                    if type(r) == ac_server_protocol.EnableRealtimeReport:
                        if not (self.realtimeReport is None or r.intervalMS < self.realtimeReport):
                            # do not pass the request, stay at higher frequency
                            continue
                    self.acSocket.sendto(data, (self.host, self.sendPort))
            except socket.timeout:
                pass
            
# just print all attributes of the event
def print_event(x, v = None, indent = "  "):
    if hasattr(x, "__dict__"):
        for a in x.__dict__:
            print_event(a, getattr(x,a, None), indent + "  ")
    else:
        s = indent + str(x) + " = "
        if type(v) in [tuple, list] and len(v) > 0 and type(v[0]) not in [float, int, str]:
            print(s)
            indent += "  "
            for e in v:
                print(indent+"-")
                print_event(e, None, indent)
        else:
            if not v is None: s += str(v)
            print(s)
                
if __name__ == "__main__":
    # example usage
    import sys, time
    from ac_server_protocol import *
    
    # this callback will be called after receiving an AC event
    def callback(event):
    
        if type(event) == NewSession:
            print("NewSession:")
            s.enableRealtimeReport(1000)
            # access the event data with 
            # event.name, event.laps, etc. see the ac_server_protocol.py for the event definitions
            # in this example we print all available attributes of the received events
        elif type(event) == CollisionEnv:
            print("CollisionEnv")
        elif type(event) == CollisionCar:
            print("CollisionCar")
        elif type(event) == CarInfo:
            print("CarInfo")
        elif type(event) == CarUpdate:
            print("CarUpdate")
        elif type(event) == NewConnection:
            print("NewConnection")
        elif type(event) == ConnectionClosed:
            print("ConnectionClosed")
        elif type(event) == LapCompleted:
            print("LapCompleted")
        print_event(event)

    # create the server plugin handling the UDP interface
    s = ACServerPlugin(int(sys.argv[1]), int(sys.argv[2]), callback, 
                       None if len(sys.argv)<4 else int(sys.argv[3]), 
                       None if len(sys.argv)<5 else int(sys.argv[4]))
    # request the car info packet of carId=7
    s.getCarInfo(7)
    # process server packets for 5 seconds
    s.processServerPackets(5)
    # enable the realtime report with 1000ms cycle time (1Hz)
    s.enableRealtimeReport(1000)
    # process server packets for 5 seconds
    s.processServerPackets(5)
    # send chat message to carId=7
    s.sendChat(7, "Hello 7")
    # process server packets for 5 seconds    
    s.processServerPackets(5)
    # send chat message to all cars
    s.broadcastChat("Hello all")
    # process server packets forever 
    s.processServerPackets()
    