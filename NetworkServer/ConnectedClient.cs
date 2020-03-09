using System.Collections.Generic;
using System.Net.Sockets;

namespace NetworkGameServer
{
    public class ConnectedClient
    {
        public int Id { get; private set; }
        public string PlayerName { get; set; }
        public Socket Connection { get; private set; }
        
        public Dictionary<int, NetworkObject> NetworkObjects { get; private set; } 

        public ConnectedClient(int id, Socket connection)
        {
            Id = id;
            Connection = connection;
            NetworkObjects = new Dictionary<int, NetworkObject>();
        }
    }
}