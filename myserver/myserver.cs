using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace httpServer
{
    public class ServerMain
    {
        int port;
        string szIP = "127.0.0.1"; ///Local machine by default 127.0.0.1
        string szFileName = "", szOperation = "";
        /// File name that will be saved on the server on the path where the myserver.exe is present
        const string SaveFileName = "DownloadedFile.txt"; 

        public ServerMain(int port)
        {
            this.port = port;
        }

        private void replyRequest(string szReply, NetworkStream nStream)
        {
            try
            {
                BinaryWriter bWriter = new BinaryWriter(nStream);
                bWriter.Write(szReply);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
        }

        private string handleGETRequest(string szFileName)
        {
            string szGETReply = "";
            
            try
            {
                if (szFileName != "")
                {
                    if (File.Exists(szFileName))
                    {
                        string szFileContent = File.ReadAllText(szFileName);
                        szGETReply = "200 OK";
                    }
                    else
                    {
                        Console.WriteLine("Specified file : {0} not present ", szFileName);
                        szGETReply = "404 Not Found";
                    }                    
                }
                else
                {
                    throw new Exception("Empty File name");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
            return szGETReply;            
        }

        private string handlePUTRequest(string szFileName)
        {
            string szPUTReply = "";
            try
            {
                szPUTReply = "200 OK FILE CREATED";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
                szPUTReply = "204 FILE NOT SAVED";
            }
            return szPUTReply;
        }

        private string saveFile(int port, NetworkStream nStream, TcpClient tClient, TcpListener listener)
        {
            string szPUTReply = "200 OK FILE CREATED";
            int iBufferSize = 1024;

            try
            {
                Console.WriteLine("Saving the file...");
                
                byte[] RecData = new byte[iBufferSize];
                int iRecBytes = 0;
                
                nStream = tClient.GetStream();
                int iTotalRecBytes = 0;

                ///Saves the file in the current directory of the .exe file
                FileStream Fs = new FileStream(SaveFileName, FileMode.OpenOrCreate, FileAccess.Write);

                while ((iRecBytes = nStream.Read(RecData, 0, RecData.Length)) > 0)
                {
                    Fs.Write(RecData, 0, iRecBytes);
                    iTotalRecBytes += iRecBytes;
                }

                Console.WriteLine("File saved successfully...");
                Fs.Close();                
            }
            catch (Exception ex)
            {
                szPUTReply = "204 FILE NOT SAVED";
                Console.WriteLine("Error while saving the file...Exception occured: " + ex.Message);
            }
            
            return szPUTReply;
        }

        private static readonly Regex rx = new Regex(@"[a-z]:\\(?:[^\\:]+\\)*((?:[^:\\]+)\.\w+)", RegexOptions.IgnoreCase);

        private static string extractFilePath(string text)
        {
            string szInp = "";
            MatchCollection matches = rx.Matches(text);            
            foreach (Match match in matches)
            {
                szInp = match.Value;
            }
            return szInp;
        }
        
        private int parseRequest(String request, NetworkStream nStream, TcpClient tClient, TcpListener listener)
        {
            int iRet = 0;
            try
            {
                Console.WriteLine(request + " was received...");

                string[] tokens = request.Split(' ');

                szIP = tokens[0].ToUpper();
                port = int.Parse(tokens[1]);
                szFileName = extractFilePath(request);

                szOperation = tokens[2];

                if (szOperation.ToUpper().Equals("GET"))
                {
                    string szGETReply = handleGETRequest(szFileName);
                    replyRequest(szGETReply, nStream);
                    Console.WriteLine(szGETReply + " was sent...");
                }
                else if (szOperation.ToUpper().Equals("PUT"))
                {
                    string szPUTReply = "";
                    szPUTReply = handlePUTRequest(szFileName);
                    replyRequest(szPUTReply, nStream);
                    szPUTReply = saveFile(port, nStream, tClient, listener);
                    Console.WriteLine(szPUTReply + " was sent...");
                }
                else if (szOperation.ToUpper().Equals("QUIT"))
                {
                    iRet = 1;
                }
                else
                {
                    throw new Exception("invalid operation request");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }

            return iRet;
        }

        public void handleRequest()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Parse(szIP), port);

                ///Starting the listner
                listener.Start();
                
                ///Continous loop
                while (true)
                {
                    Console.WriteLine("\nWaiting for connections...");
                    
                    ///Accept client connection
                    using (TcpClient tClient = listener.AcceptTcpClient())
                    {
                        Console.WriteLine("Connection Request accepted...");
                        
                        using (NetworkStream nStream = tClient.GetStream())
                        {
                            BinaryReader bReader = new BinaryReader(nStream);
                            string szReceived = bReader.ReadString();
                            
                            int iRet = parseRequest(szReceived, nStream, tClient, listener);
                            if (iRet == 1)
                            {
                                tClient.Close();
                                Console.WriteLine("QUIT request received, gracefully exiting in 3 sec...");

                                ///3 sec sleep for displaying the message to the user...
                                Thread.Sleep(3000);
                                break;
                            }
                        }

                        tClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
        }

        public static int Main(String[] args)
        {
            try
            {
                ServerMain sMain;

                if (args.GetLength(0) != 1)
                {
                    Console.WriteLine("Invalid input..");
                    return 0;
                }
                
                sMain = new ServerMain(Convert.ToInt16(args[0]));

                ///Multithreaded call to handleRequest function
                Thread thread = new Thread(sMain.handleRequest);
                thread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message);
            }
            return 0;
        }
    }
}