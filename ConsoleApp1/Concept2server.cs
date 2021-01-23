using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Concept2api;
using CSAFE_Fitness;

namespace Concept2Server
{
    class Program
    {
        static C2ServerHandler _C2Srv;

        static void Main(string[] args)
        {
            _C2Srv = new C2ServerHandler();

            while (_C2Srv.DeviceCount <= 0)
            {
                Console.WriteLine("[Concept2server] No Concept2 PMs available.  Retrying in a few seconds.");
                Thread.Sleep(5000);
                _C2Srv.DiscoverDevice();
            }

            int cntStreams = _C2Srv.OpenStream();
            while(cntStreams <= 0)
            {
                Console.WriteLine("[Concept2server] No Concept2 Data Streams.  Retrying in a few seconds.");
                Thread.Sleep(5000);
                cntStreams = _C2Srv.OpenStream();
            }
            Console.WriteLine("[Concept2server] Opened Concept2 Data Stream.", cntStreams);



            Console.WriteLine("[Concept2server] Starting Server");
            ExecuteServer();

            Console.WriteLine("[Concept2server] Press Any Key to Exit");
            Console.ReadKey();
        }


        public static void ExecuteServer()
        {
            // Establish the local endpoint  
            // for the socket. Dns.GetHostName 
            // returns the name of the host  
            // running the application. 
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

            // Creation TCP/IP Socket using  
            // Socket Class Costructor 
            Socket listener = new Socket(ipAddr.AddressFamily,
                         SocketType.Stream, ProtocolType.Tcp);

            try
            {

                // Using Bind() method we associate a 
                // network address to the Server Socket 
                // All client that will connect to this  
                // Server Socket must know this network 
                // Address 
                listener.Bind(localEndPoint);

                // Using Listen() method we create  
                // the Client list that will want 
                // to connect to Server 
                listener.Listen(10);

                while (true)
                {

                    Console.WriteLine("[Concept2server] Waiting connection ... ");

                    // Suspend while waiting for 
                    // incoming connection Using  
                    // Accept() method the server  
                    // will accept connection of client 
                    Socket clientSocket = listener.Accept();

                    // Data buffer 
                    byte[] rxBuffer = new byte[2048];
                    byte[] rxData;
                    byte[] txData;

                    Console.WriteLine("[Concept2server] Reading Data");

                    int numByte = clientSocket.Receive(rxBuffer);
                    rxData = new byte[numByte];
                    Array.Copy(rxBuffer, rxData, numByte);


                    if (!_C2Srv.HandleClientRequest(rxData, out txData))
                    {
                        Console.WriteLine("[Concept2server] Could not handle Client Data!");
                    }

                    // Send a message to Client  
                    // using Send() method 
                    clientSocket.Send(txData);

                    // Close client Socket using the 
                    // Close() method. After closing, 
                    // we can use the closed Socket  
                    // for a new Client Connection 
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }



    }
}
