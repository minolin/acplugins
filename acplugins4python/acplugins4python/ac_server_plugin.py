"""
Copyright (c) 2015, NeverEatYellowSnow (NEYS)
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. All advertising materials mentioning features or use of this software
   must display the following acknowledgement:
   This product includes software developed from NeverEatYellowSnow (NEYS).
4. Neither the name of NeverEatYellowSnow (NEYS) nor the
   names of its contributors may be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY <COPYRIGHT HOLDER> ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
"""

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
        self.rcvPort = rcvPort
        self.acSocket = self.openSocket(self.host, self.rcvPort, self.sendPort, None)
        
        
        if not proxyRcvPort is None and not proxySendPort is None:
            self.proxyRcvPort = proxyRcvPort
            self.proxySendPort = proxySendPort
            self.proxySocket = self.openSocket(self.host, self.proxySendPort, self.proxyRcvPort, None)
            self.proxySocketThread = Thread(target=self._performProxy)
            self.proxySocketThread.daemon=True
            self.proxySocketThread.start()
        else:
            self.proxySocket = None
            
        self.realtimeReport = None
                    
    def openSocket(self, host, rcvp, sendp, s):
        if not s is None: s.close()        
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.bind( (host, rcvp) )
        # set up a 0.5s pulse, need this to be able to Ctrl-C the python apps
        s.settimeout(0.5)
        return s

    def processServerPackets(self, timeout=None):
        """
        call this function to process server packets for a given
        timespan (timeout) or forever, if timeout is set to None
        """
        t = time.time()
        while 1:
            try:
                data, addr = self.acSocket.recvfrom(2048)
                #data = self.acSocket.recv(2048)
                if not self.proxySocket is None:
                    try:
                        self.proxySocket.sendto(data, ("127.0.0.1", self.proxyRcvPort))
                    except:
                        pass
                r = ac_server_protocol.parse(data)
                for c in self.callbacks:
                    c(r)
            except socket.timeout:
                pass
            except ConnectionResetError:
                # I hate windows :( who would ever get the idea to set WSAECONNRESET on a connectionless protocol ?!?
                # The upshot is this: when we send data to a socket which has no listener attached (yet)
                # windows is giving the connection reset by peer error at the next recv call
                # It seems that the socket is unusable afterwards, so we re-open it :(
                self.acSocket = self.openSocket(self.host, self.rcvPort, self.sendPort, self.acSocket)
            if not timeout is None:
                if time.time()-t > timeout:
                    break
                    
    def getSessionInfo(self, sessionIndex = -1):
        """
        request the session info of the specified session (-1 for current session)
        """
        p = ac_server_protocol.GetSessionInfo(sessionIndex=sessionIndex)
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))        
   
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
        
    def setSessionInfo(self, sessionIndex, sessionName, sessionType, laps, timeSeconds, waitTimeSeconds):
        p = ac_server_protocol.SetSessionInfo(sessionIndex=sessionIndex,
                                              sessionName=sessionName,
                                              sessionType=sessionType,
                                              laps=laps,
                                              timeSeconds=timeSeconds,
                                              waitTimeSeconds=waitTimeSeconds)
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
        
    def kickUser(self, carId):
        p = ac_server_protocol.KickUser(carId=carId)
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
        
    def nextSession(self):
        p = ac_server_protocol.NextSession()
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
        
    def restartSession(self):
        p = ac_server_protocol.RestartSession()
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))

    def adminCommand(self, command):
        p = ac_server_protocol.AdminCommand(command=command)
        self.acSocket.sendto(p.to_buffer(), (self.host, self.sendPort))
        
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
            except ConnectionResetError:
                # I hate windows :( who would ever get the idea to set WSAECONNRESET on a connectionless protocol ?!?
                self.proxySocket = self.openSocket(self.host, self.proxySendPort, self.proxyRcvPort, self.proxySocket)
                time.sleep(1.0)
                
            
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
                
