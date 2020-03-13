using System;
using System.Threading;
using NetworkGameServer.Logger;

namespace NetworkGameServer
{
    class Program
    {
        // IP address and port to server start on
        private const string IP = "127.0.0.1";
        private const int Port = 9000;
        private static Timer _serverLoopTimer;
        private static Server _server;
        private static ILogger _logger;
        
        static void Main(string[] args)
        {
            _logger = new TimestampLogger();
            _server = new Server();
            _server.Start(IP, Port);
            _serverLoopTimer = SimpleTimer.Start(ServerLoop, Constants.TIME_BETWEEN_TICK, true);
            Console.ReadKey();
        }

        private static void ServerLoop()
        {
            _server.Update();
        }
    }
}