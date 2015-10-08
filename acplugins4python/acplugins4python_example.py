#!/usr/bin/env python

if __name__ == "__main__":
    # example usage
    import sys, time
    from acplugins4python.ac_server_protocol import *
    from acplugins4python.ac_server_plugin import *
    sessionInfo = None
    # this callback will be called after receiving an AC event
    def callback(event):
        global sessionInfo
        if type(event) in [NewSession, SessionInfo]:
            sessionInfo = event
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
        elif type(event) == EndSession:
            print("EndSession")
        elif type(event) == ClientLoaded:
            print("ClientLoaded")
        elif type(event) == ChatEvent:
            print("ChatEvent")
        elif type(event) == ProtocolVersion:
            print("ProtocolVersion")
        elif type(event) == ProtocolError:
            print("ProtocolError")
        else:
            print("<unknown event>", type(event))
        print_event(event)

    # create the server plugin handling the UDP interface
    s = ACServerPlugin(int(sys.argv[1]), int(sys.argv[2]), callback, 
                       None if len(sys.argv)<4 else int(sys.argv[3]), 
                       None if len(sys.argv)<5 else int(sys.argv[4]))
    print("Get session info for current session")
    s.getSessionInfo() # get the session info for the current session                   
    # request the car info packet of carId=7
    print("Get car info for car 7")
    s.getCarInfo(7)
    # process server packets for 5 seconds
    s.processServerPackets(2)
    print("Enable RT Report")
    # enable the realtime report with 1000ms cycle time (1Hz)
    s.enableRealtimeReport(1000)
    # process server packets for 5 seconds
    s.processServerPackets(2)
    print("sending chat to car 7")
    # send chat message to carId=7
    s.sendChat(7, "Hello 7")
    # process server packets for 5 seconds    
    s.processServerPackets(2)
    print("broadcasting chat to all cars")
    # send chat message to all cars
    s.broadcastChat("Hello all")
    # process server packets forever 
    s.processServerPackets(2)
    print("kicking user 7")
    s.kickUser(7)
    s.processServerPackets(2)
    print("setting next session to a race session, 10 laps")
    if sessionInfo is None:
        s.getSessionInfo()
        s.processServerPackets(1)
    si = sessionInfo
    s.setSessionInfo(sessionIndex=(si.currSessionIndex + 1)%si.sessionCount, 
                     sessionName="My new race session", 
                     sessionType=2, 
                     laps=10, 
                     timeSeconds=0, 
                     waitTimeSeconds=20)
    s.processServerPackets(1)
    print("move on to next session")
    s.nextSession()
    s.processServerPackets(5)
    print("restart this session")
    s.restartSession()
    s.processServerPackets(5)
    print("restart this session (using admin command)")
    s.adminCommand("/restart_session")
    s.processServerPackets()
    