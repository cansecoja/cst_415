// PRSServerProgram.cs
//
// Juan Canseco
// CST 415
// Fall 2020
// 

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using PRSLib;

namespace PRSServer
{
    class PRSServerProgram
    {
        class PRS
        {
            // represents a PRS Server, keeps all state and processes messages accordingly

            class PortReservation
            {
                private ushort port;
                private bool available;
                private string serviceName;
                private DateTime lastAlive;

                public PortReservation(ushort port)
                {
                    this.port = port;
                    available = true;
                }

                public string ServiceName { get { return serviceName; } }
                public ushort Port { get { return port; } }
                public bool Available { get { return available; } }

                public bool Expired(int timeout)
                {
                    // TODO: PortReservation.Expired()
                    // return true if timeout secons have elapsed since lastAlive

                    return false;
                }

                public void Reserve(string serviceName)
                {
                    // reserve this port for serviceName
                    available = false;
                    this.serviceName = serviceName;
                    lastAlive = DateTime.Now;

                }

                public void KeepAlive()
                {
                    // save current time in lastAlive
                    lastAlive = DateTime.Now;
                }

                public void Close()
                {
                    // make this reservation available
                    available = true;
                    serviceName = null;

                }
            }

            // server attribues
            private ushort startingClientPort;
            private ushort endingClientPort;
            private int keepAliveTimeout;
            private int numPorts;
            private PortReservation[] ports;
            private bool stopped;

            public PRS(ushort startingClientPort, ushort endingClientPort, int keepAliveTimeout)
            {
                // save parameters
                this.startingClientPort = startingClientPort;
                this.endingClientPort = endingClientPort;
                this.keepAliveTimeout = keepAliveTimeout;

                // initialize to not stopped
                stopped = false;

                // initialize port reservations
                numPorts = endingClientPort - startingClientPort + 1;  //inclusive
                ports = new PortReservation[numPorts];

                //loop throughthe port reservation array, filling in the port #'s
                for (ushort port = startingClientPort; port <= endingClientPort; port++)
                {
                    // the array is zero-based index, port #'s start at startingClientPort
                    ports[port - startingClientPort] = new PortReservation(port);
                }
            }

            public bool Stopped { get { return stopped; } }

            private void CheckForExpiredPorts()
            {
                // TODO: PRS.CheckForExpiredPorts()
                // expire any ports that have not been kept alive

            }

            private PRSMessage RequestPort(string serviceName)
            {
                PRSMessage response = null;

                // validate that serviceNmae is not already reserved, if it is, send SERVICE_IN_USE
                if (ports.SingleOrDefault(p => p.ServiceName == serviceName && !p.Available) == null)
                {
                    // client has requested the lowest available port, so find it!
                    PortReservation reservation = ports.FirstOrDefault(p => p.Available);

                    // if found an avialable port, reserve it and send SUCCESS
                    // else, none available, send ALL_PORTS_BUSY
                    if (reservation != null)
                    {
                        reservation.Reserve(serviceName);
                        response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, reservation.Port, PRSMessage.STATUS.SUCCESS);
                    }
                    else
                    {
                        response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, 0, PRSMessage.STATUS.ALL_PORTS_BUSY);
                    }
                }
                else
                {
                    response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, 0, PRSMessage.STATUS.SERVICE_IN_USE);
                }
                return response;
            }

            public PRSMessage HandleMessage(PRSMessage msg)
            {
                // handle one message and return a response

                PRSMessage response = null;

                switch (msg.MsgType)
                {
                    case PRSMessage.MESSAGE_TYPE.REQUEST_PORT:
                        {
                            // TODO: check for expired ports
                            // try to reserve requested port send requested report back in response
                            response = RequestPort(msg.ServiceName);
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.KEEP_ALIVE:
                        {
                            // client has requested that we keep their port alive
                            // find the reserve port by port# and service name
                            PortReservation reservation = ports.FirstOrDefault(p => !p.Available && p.ServiceName == msg.ServiceName && p.Port == msg.Port);

                            // if found, keep it alive and send SUCCESS
                            if (reservation != null)
                            {
                                reservation.KeepAlive();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            else
                            {
                                // else, SERVICE_NOT_FOUND
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.CLOSE_PORT:
                        {
                            // client has requested that we close their port, and make it available for others!
                            // find the port
                            // if found, close it and send SUCCESS
                            // else, SERVICE_NOT_FOUND

                            PortReservation reservation = ports.FirstOrDefault(p => !p.Available && p.ServiceName == msg.ServiceName && p.Port == msg.Port);

                            // if found, close and send SUCCESS
                            if (reservation != null)
                            {
                                reservation.KeepAlive();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            else
                            {
                                // else, SERVICE_NOT_FOUND
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.LOOKUP_PORT:
                        {
                            // client wants to know the reserved port number for a named service
                            // find the port
                            // if found, send port number back
                            // else, SERVICE_NOT_FOUND
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.STOP:
                        {
                            // client is telling us to close the appliation down
                            // stop the PRS and return SUCCESS
                        }
                        break;
                }

                return response;
            }

        }

        static void Usage()
        {
            Console.WriteLine("usage: PRSServer [options]");
            Console.WriteLine("\t-p < service port >");
            Console.WriteLine("\t-s < starting client port number >");
            Console.WriteLine("\t-e < ending client port number >");
            Console.WriteLine("\t-t < keep alive time in seconds >");
        }

        static void Main(string[] args)
        {
            // defaults
            ushort SERVER_PORT = 30000;
            ushort STARTING_CLIENT_PORT = 40000;
            ushort ENDING_CLIENT_PORT = 40099;
            int KEEP_ALIVE_TIMEOUT = 300;

            // TODO: process command options
            // -p < service port >
            // -s < starting client port number >
            // -e < ending client port number >
            // -t < keep alive time in seconds >

            // TODO: check for valid STARTING_CLIENT_PORT and ENDING_CLIENT_PORT

            // initialize the PRS server
            PRS prs = new PRS(STARTING_CLIENT_PORT, ENDING_CLIENT_PORT, KEEP_ALIVE_TIMEOUT);

            // create the socket for receiving messages at the server
            Socket listeningSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            // bind the listening socket to the PRS server port
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));

            //
            // Process client messages
            //

            while (!prs.Stopped)
            {
                EndPoint clientEndPoint = null;
                try
                {
                    // receive a message from a client
                    clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    PRSMessage msg = PRSMessage.ReceiveMessage(listeningSocket, ref clientEndPoint);

                    // let the PRS handle the message
                    PRSMessage response = prs.HandleMessage(msg);

                    // send response message back to client
                    response.SendMessage(listeningSocket, clientEndPoint);
                }
                catch (Exception ex)
                {
                    // attempt to send a UNDEFINED_ERROR response to the client, if we know who that was
                    if (clientEndPoint != null)
                    {
                        PRSMessage errorMsg = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, "", 0, PRSMessage.STATUS.UNDEFINED_ERROR);
                        errorMsg.SendMessage(listeningSocket, clientEndPoint);
                    }
                }
            }

            // close the listening socket
            listeningSocket.Close();
            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
