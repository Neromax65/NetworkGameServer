using System;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;

namespace NetworkClient
{
    class Program
    {
        // IP address and port of server to connect
        private const string IP = "127.0.0.1";
        private const int Port = 9000;

        private static void Main(string[] args)
        {
            Client client = new Client();
            client.Connect(IP, Port);
            
            // try
            // {
            //     IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
            //     Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //     
            //     // Connecting to the server
            //     _logger.Log($"[{DateTime.Now.ToShortTimeString()}] Trying to connect to {IP}:{Port}...");
            //     socket.Connect(ipEndPoint);
            //     _logger.Log($"[{DateTime.Now.ToShortTimeString()}] Successfully connected to {IP}:{Port}");
            //
            //     AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            //     {
            //         _logger.Log("Exit...");
            //         socket.Disconnect(false);
            //         socket.Shutdown(SocketShutdown.Both);
            //         socket.Close();
            //     };
            //     
            //     // Reading the message we want to send to server
            //     _logger.Log($"[{DateTime.Now.ToShortTimeString()}] Enter message: ");
            //     string message = Console.ReadLine();
            //     
            //     // Translate message into byte array
            //     byte[] data = Encoding.Unicode.GetBytes(message);
            //     
            //     // Send message to the server
            //     socket.Send(data);
            //     
            //     data = new byte[256];
            //     StringBuilder builder = new StringBuilder();
            //     do
            //     {
            //         // Receiving data from the server
            //         var bytes = socket.Receive(data, data.Length, 0);
            //         builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            //     } while (socket.Available > 0);
            //     _logger.Log($"[{DateTime.Now.ToShortTimeString()}] Server response: {builder}");
            //     
            //     // Disconnecting from the server
            //     socket.Shutdown(SocketShutdown.Both);
            //     socket.Close();
            //     
            // }
            // catch (Exception ex)
            // {
            //     _logger.Log($"[{DateTime.Now.ToShortTimeString()}] NETWORK ERROR: {ex}");
            //     throw;
            // }

            Console.Read();
        }
    }
}