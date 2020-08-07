using System.Collections.Generic;
using System.Net.Sockets;

namespace NetworkGameServer
{
    /// <summary>
    /// Class, that contains all information of connected client
    /// </summary>
    public class ConnectedClient
    {
        /// <summary>
        /// Client network identity
        /// </summary>
        public int Id { get; private set; }
        
        /// <summary>
        /// Client name
        /// </summary>
        public string PlayerName { get; set; }
        
        /// <summary>
        /// Socket, that handles connection 
        /// </summary>
        public Socket Connection { get; private set; }
        
        /// <summary>
        /// Counter for not receiving any messages from client
        /// </summary>
        public int PingFailure { get; set; }
        
        /// <summary>
        /// Is currently connected
        /// </summary>
        public bool Connected { get; set; }
        
        /// <summary>
        /// Network object, that belongs to this client
        /// </summary>
        public Dictionary<int, NetworkObject> NetworkObjects { get; private set; } 

        public ConnectedClient(int id, Socket connection)
        {
            Id = id;
            Connection = connection;
            NetworkObjects = new Dictionary<int, NetworkObject>();
        }
    }
}