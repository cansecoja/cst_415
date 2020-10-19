﻿// FTServerProgram.cs
//
// Juan Canseco
// CST 415
// Fall 2020
// 

using System;
using PRSLib;


namespace FTServer
{
    class FTServerProgram
    {
        private static void Usage()
        {
            Console.WriteLine("Usage: FTServer -prs <PRS IP address>:<PRS port>");
        }

        static void Main(string[] args)
        {
            // defaults
            ushort FTSERVER_PORT = 40000;
            int CLIENT_BACKLOG = 5;
            string PRS_ADDRESS = "127.0.0.1";
            ushort PRS_PORT = 30000;
            string SERVICE_NAME = "FT Server";

            // process the command line arguments to get the PRS ip address and PRS port number
            try
            {
                if (args.Length > 0)
                {
                    if (args[0] == "-prs")
                    {
                        string[] IP_PORT = args[1].Split(':');

                        PRS_ADDRESS = IP_PORT[0];
                        PRS_PORT = ushort.Parse(IP_PORT[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("PRS Address: " + PRS_ADDRESS);
            Console.WriteLine("PRS Port: " + PRS_PORT);
            
            try
            {
                // contact the PRS, request a port for "FT Server" and start keeping it alive
                PRSClient prs = new PRSClient(PRS_ADDRESS, PRS_PORT, SERVICE_NAME);
                FTSERVER_PORT = prs.RequestPort();
                prs.KeepPortAlive();

                // instantiate FT server and start it running
                FTServer ft = new FTServer(FTSERVER_PORT, CLIENT_BACKLOG);
                ft.Start();

                // tell the PRS that it can have it's port back, we don't need it anymore
                prs.ClosePort();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
