import socket, time, select
import ac_server_protocol

class ACServerPlugin:

    def __init__(self, rcvPort, sendPort, callbacks, serverIP="127.0.0.1"):
        """
        create a new server plugin instance, given 
            - rcvPort  : used to receive the messages from the AC server
            - sendPort : used to send requests to the AC server
            - callbacks: a list of callables (or a single callable) called 
                         whenever a new message is received from the server
                         the function receives one argument with an instance
                         of the data sent by the server. 
                         (see ac_server_protocol for details)
        """
        try:
            _ = iter(callbacks)
        except TypeError:
            callbacks = [callbacks]
        self.callbacks = callbacks 
        
        self.sendSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sendSocket.connect( (serverIP, sendPort) )
        
        self.rcvSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.rcvSocket.bind( (serverIP, rcvPort) )
        # set up a 0.5s pulse, need this to be able to Ctrl-C the python apps
        self.rcvSocket.settimeout(0.5)
        
    def processServerPackets(self, timeout=None):
        """
        call this function to process server packets for a given
        timespan (timeout) or forever, if timeout is set to None
        """
        t = time.time()
        while 1:
            try:
                data, addr = self.rcvSocket.recvfrom(2048)
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
        self.sendSocket.send(p.to_buffer())
    
    def enableRealtimeReport(self, intervalMS):
        """
        enable the realtime report with a given interval
        """
        p = ac_server_protocol.EnableRealtimeReport(intervalMS=intervalMS)
        self.sendSocket.send(p.to_buffer())
        
    def sendChat(self, carId, message):
        """
        send chat message to a specific car
        """
        p = ac_server_protocol.SendChat(carId=carId, message=message)
        self.sendSocket.send(p.to_buffer())
        
    def broadcastChat(self, message):
        """
        broadcast chat message to all cars
        """
        p = ac_server_protocol.BroadcastChat(message=message)
        self.sendSocket.send(p.to_buffer())
    
if __name__ == "__main__":
    # example usage
    import sys, time
    from ac_server_protocol import *
    
    # this callback will be called after receiving an AC event
    def callback(event):
    
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
                        
        if type(event) == NewSession:
            print("NewSession:")
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
    s = ACServerPlugin(int(sys.argv[1]), int(sys.argv[2]), callback)
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
    