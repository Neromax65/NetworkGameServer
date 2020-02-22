namespace NetworkGameServer
{
    class Program
    {
        // IP address and port to server start on
        private const string IP = "127.0.0.1";
        private const int Port = 9000;
        
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start(IP, Port);
        }
    }
}