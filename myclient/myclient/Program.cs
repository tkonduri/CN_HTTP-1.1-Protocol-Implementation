using System;
using System.IO;
using System.Net.Sockets;

namespace myclient
{
    class clientOperations
    {
        private static void connectToServerUsingTcp(string host, int port, string operation, string path)
        {
            try
            {
                string szMsg = host + " " + Convert.ToString(port) + " " + operation + " " + @path;
                string szReceived = "";
                int iRet = 0;

                using (TcpClient tClient = new TcpClient(host, port))
                {
                    Console.WriteLine("Connection was established...");

                    using (NetworkStream nStream = tClient.GetStream())
                    {
                        Console.WriteLine("Stream was received from the connection...");

                        ///Sending a message
                        BinaryWriter bWriter = new BinaryWriter(nStream);
                        bWriter.Write(szMsg);
                        Console.WriteLine(szMsg + " was sent...");
                        
                        if (true != String.Equals("QUIT", operation.ToUpper()))
                        {
                            ///Receiving a message and displaying it
                            BinaryReader bReader = new BinaryReader(nStream);
                            szReceived = bReader.ReadString();
                        }

                        if (String.Equals("PUT", operation.ToUpper()))
                        {
                            iRet = sendFile(host, port, path, tClient, nStream, bWriter);                            
                        }
                        
                        if ((true != String.Equals("QUIT", operation.ToUpper())) && 1 != iRet)
                        {
                            Console.WriteLine(szReceived + " was received...");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static int sendFile(string ip, int port, string path, TcpClient tClient, NetworkStream nStream, BinaryWriter bWriter)
        {
            int iRet = 0;
            try
            {
                Console.WriteLine("Sending file " + path + " ...");

                byte[] SendingBuffer = null;

                FileStream Fs = new FileStream(path, FileMode.Open, FileAccess.Read);

                int iBufferSize = 1024;
                int iNoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(iBufferSize)));
                int iTotalLength = (int)Fs.Length, iCounter = 0, iCurrentPacketLength = 0, iCtr = 0;

                for (iCounter = 0; iCounter < iNoOfPackets; iCounter++)
                {
                    if (iBufferSize < iTotalLength)
                    {
                        iCurrentPacketLength = iBufferSize;
                        iTotalLength = iTotalLength - iCurrentPacketLength;
                    }
                    else
                    {
                        iCurrentPacketLength = iTotalLength;
                    }

                    SendingBuffer = new byte[iCurrentPacketLength];
                    Fs.Read(SendingBuffer, 0, iCurrentPacketLength);

                    bWriter = new BinaryWriter(nStream);
                    bWriter.Write(SendingBuffer, 0, (int)SendingBuffer.Length);

                    iCtr += (int)SendingBuffer.Length;
                    Console.WriteLine(iCtr + " byte were written/sent ...");                    
                }
                Fs.Close();                
            }
            catch (Exception ex)
            {
                iRet = 1;
                Console.WriteLine("Exception occured: " + ex.Message);
            }
            return iRet;
        }

        public static void Main(string[] args)
        {
            try
            {
                string path = "";
                if ((args.Length > 4) || (args.Length < 3))
                {
                    Console.WriteLine("Wrong inputs...");
                    return;
                }

                ///Fetching the inputs from command line
                var host = args[0];
                Console.WriteLine("Host = " + host);

                var operation = args[2];
                Console.WriteLine("Operation = " + operation);

                int port = 0;
                int.TryParse(args[1], out port);
                Console.WriteLine("Port = " + port);

                if (args.Length != 3)
                {
                    path = @args[3];
                    Console.WriteLine("Path = " + path);                    
                }

                if ((0 != string.Compare(operation.ToUpper(), "PUT")) && (0 != string.Compare(operation.ToUpper(), "GET")) && (0 != string.Compare(operation.ToUpper(), "QUIT")))
                {
                    Console.WriteLine("Invalid command: {0}", operation.ToUpper());
                    return;
                }

                if ((!File.Exists(path)) && (0 == string.Compare(operation.ToUpper(), "PUT")))
                {
                    Console.WriteLine("File path {0} doesn't exist...", path);
                    return;
                }

                 ///Setting up connection with the server, and sending requests and receiving response...
                 connectToServerUsingTcp(host, port, operation.ToUpper(), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
        }
    }
}