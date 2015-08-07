#!/usr/bin/env python

if __name__ == "__main__":
    # example usage
    import sys, time
    from acplugins4python.ac_server_protocol import *
    from acplugins4python.ac_server_plugin import *
    
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
    