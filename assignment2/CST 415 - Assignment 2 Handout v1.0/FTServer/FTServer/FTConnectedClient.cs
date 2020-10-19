// FTConnectedClient.cs
//
// Juan Canseco
// CST 415
// Fall 2020
// 

using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace FTServer
{
    class FTConnectedClient
    {
        // represents a single connected ft client, that wants directory contents from the server
        // each client will have its own socket and thread
        // client is given it's socket from the FTServer when the server accepts the connection
        // the client class creates it's own thread
        // the client's thread will process messages on the client's socket

        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;

        public FTConnectedClient(Socket clientSocket)
        {
            // save the client's socket
            this.clientSocket = clientSocket;

            // at this time, there is no stream, reader, write or thread
            stream = null;
            reader = null;
            writer = null;
            clientThread = null;
        }

        public void Start()
        {
            // called by the main thread to start the clientThread and process messages for the client

            // create and start the clientThread, pass in a reference to this class instance as a parameter
            clientThread = new Thread(ThreadProc);
            clientThread.Start(this);
        }

        private static void ThreadProc(Object param)
        {
            // the procedure for the clientThread
            // when this method returns, the clientThread will exit

            // the param is a FTConnectedClient instance
            // start processing messages with the Run() method
            FTConnectedClient client = param as FTConnectedClient;
            client.Run();
        }

        private void Run()
        {
            // this method is executed on the clientThread

            try
            {
                // create network stream, reader and writer over the socket
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                // process client requests
                bool done = false;
                while (!done)
                {
                    // receive a message from the client
                    string msg = reader.ReadLine();

                    // handle the message
                    if (msg == "get")
                    {
                        // get directoryName
                        string directoryName = reader.ReadLine();

                        // retrieve directory contents and sending all the files

                        // if directory does not exist! send an error!
                        if (!Directory.Exists(directoryName))
                        {
                            SendError("Directory does not exist :" + directoryName);
                        }
                        else
                        {
                            // directory exists, so send each file to the client
                            // for each file...
                            foreach (string fName in Directory.GetFiles(directoryName))
                            {
                                // make sure it's a txt file
                                FileInfo fi = new FileInfo(fName);
                                if (fi.Extension == ".txt")
                                {
                                    // send file name and length
                                    SendFileName(fi.Name, (int)fi.Length);

                                    // get the file contents
                                    string contents = File.ReadAllText(fName);

                                    // send a file to the client
                                    SendFileContents(contents);
                                }
                            }

                            // send done after last file
                            SendDone();
                        }
                    }

                    else if (msg == "exit")
                    {
                        // client is done, disconnect it's socket and quit the thread
                        clientSocket.Disconnect(false);
                        done = true;
                    }
                    
                    else
                    {
                        // error handling for an invalid message
                        SendError("Unrecognized message: " + msg);

                        // this client is too broken to waste our time on!
                        // quite processing messages and disconnect
                        clientSocket.Disconnect(false);
                        done = true;
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Error on client socket, closing connection: " + se.Message);
            }

            // close the client's writer, reader, network stream and socket
            writer.Close();
            reader.Close();
            stream.Close();
            clientSocket.Close();
        }

        private void SendFileName(string fileName, int fileLength)
        {
            // send file name and file length message
            writer.Write(fileName + "\n" + fileLength.ToString() + "\n");
            writer.Flush();
        }

        private void SendFileContents(string fileContents)
        {
            // send file contents only
            // NOTE: no \n at end of contents
            writer.Write(fileContents);
            writer.Flush();
        }

        private void SendDone()
        {
            // send done message
            string done = "done\n";
            writer.Write(done);
            writer.Flush();
        }

        private void SendError(string errorMessage)
        {
            // send error message
            string err = "error\n" + errorMessage + "\n";
            writer.Write(err);
            writer.Flush();
        }
    }
}
