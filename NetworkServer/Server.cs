using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessagePack;
using NetworkGameServer.Logger;
using NetworkGameServer.NetworkData;

namespace NetworkGameServer
{
    /// <summary>
    /// Server class, that contains all information about server functionality
    /// </summary>
    public class Server
    {
        /// <summary>
        /// Socket, that will be listening to connections
        /// </summary>
        private Socket _listenSocket;
        
        /// <summary>
        /// Dictionary, that stores connected clients
        /// </summary>
        private Dictionary<int, ConnectedClient> _connectedClients;
        
        /// <summary>
        /// Dictionary, that stores all network objects
        /// </summary>
        private Dictionary<int, NetworkObject> _networkObjects;
        
        /// <summary>
        /// Dictionary, which stores all packets of data, that need to be sent
        /// </summary>
        private Dictionary<int, DataPacket> _dataPackets;
        
        /// <summary>
        /// Custom console logger
        /// </summary>
        private ILogger _logger;
        
        
        /// <summary>
        /// Initialize the server
        /// </summary>
        /// <param name="ip">IP-address</param>
        /// <param name="port">Port</param>
        public void Start(string ip, int port)
        {
            _logger = new TimestampLogger();
            _connectedClients = new Dictionary<int, ConnectedClient>();
            _networkObjects = new Dictionary<int, NetworkObject>();
            _dataPackets = new Dictionary<int, DataPacket>();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(ipEndPoint);
            _logger.Log($"Server is started on {ip}:{port}");
            _listenSocket.Listen(Constants.MAX_PENDING_CONNECTIONS);
            _logger.Log($"Listening to connections...");
        }

        /// <summary>
        /// Receive data from client
        /// </summary>
        /// <param name="client">Client to listen</param>
        private async void ReceiveData(ConnectedClient client)
        {
            if (client.Connection.Available == 0)
            {
                if (client.Connected)
                    client.PingFailure++;
                if (client.PingFailure >= 4)
                    _logger.Log($"{client.PlayerName}`s ping failure count: {client.PingFailure}");
                if (client.PingFailure >= Constants.MAX_PING_FAILURE_COUNT)
                    CloseConnection(client, DisconnectReason.PingFailure);
                return;
            }
            client.PingFailure = 0;
            byte[] buffer = new byte[Constants.BUFFER_SIZE];
            int bytes = await client.Connection.ReceiveAsync(buffer, SocketFlags.None);
            List<INetworkData> dataList = DeserializeDataPacket(buffer).DataList;
            if (dataList == null || dataList.Count == 0)
                return;
            Parallel.ForEach(dataList, data =>
            {
                HandleCommand(data.Command, data, client);
            });
            if (client.Connection != null && client.Connection.Connected)
            {
                _dataPackets[client.Id].Add(new Data_Ping());

            }
        }

        /// <summary>
        /// Recognize network data from client
        /// </summary>
        /// <param name="command">Byte of command</param>
        /// <param name="data">Network data</param>
        /// <param name="client">Network client</param>
        private void HandleCommand(byte command, INetworkData data, ConnectedClient client)
        {
            switch (command)
            {
                case Command.None:
                    _logger.Log("No command was received.");
                    break;
                case Command.Ping:
                    break;
                case Command.Position:
                    var posData = data as Data_Position;
                    _networkObjects[posData.Id].Position = posData.Position;
                    SendDataExcept(client, data);
                    break;
                case Command.Rotation:
                    var rotData = data as Data_Rotation;
                    _networkObjects[rotData.Id].Rotation = rotData.Rotation;
                    SendDataExcept(client, data);
                    break;
                case Command.Scale:
                    var sclData = data as Data_Scale;
                    _networkObjects[sclData.Id].Scale = sclData.Scale;
                    SendDataExcept(client, data);
                    break;
                case Command.Spawn:
                    SendDataExcept(client, data);
                    break;
                case Command.Connect:
                    var connectData = data as Data_Connect;
                    client.PlayerName = connectData.PlayerName;
                    client.Connected = true;
                    _logger.Log($"Player {client.PlayerName} connected.");
                    Synchronize(client);
                    break;
                case Command.Disconnect:
                    _logger.Log($"Received disconnect message from {client.PlayerName}");
                    CloseConnection(client, DisconnectReason.Manual);
                    break;
                case Command.Register:
                    var registerData = data as Data_Register;
                    var networkObject = new NetworkObject(registerData.Id, registerData.PrefabIndex,
                        registerData.OwningPlayerId);
                    if (!_networkObjects.ContainsKey(networkObject.Id))
                        _networkObjects.Add(registerData.Id, networkObject);
                    if (!_connectedClients[registerData.OwningPlayerId].NetworkObjects.ContainsKey(networkObject.Id))
                        _connectedClients[registerData.OwningPlayerId].NetworkObjects[networkObject.Id] = networkObject;
                    break;
                case Command.Unregister:
                    var unregisterData = data as Data_Unregister;
                    _networkObjects.Remove(unregisterData.Id);
                    // TODO: Temporary
                    foreach (var connectedClient in _connectedClients.Values)
                    {
                        if (connectedClient.NetworkObjects.ContainsKey(unregisterData.Id))
                            connectedClient.NetworkObjects.Remove(unregisterData.Id);
                    }
                    break;
                default:
                    _logger.Log("Unrecognized command.");
                    break;
            }
        }

        /// <summary>
        /// Synchronize all network objects with client
        /// </summary>
        /// <param name="client">Client to synchronize</param>
        private void Synchronize(ConnectedClient client)
        {
            foreach (var networkObject in _networkObjects.Values)
            {
                if (networkObject.PrefabIndex != -1)
                {
                    var spawnData = new Data_Spawn()
                    {
                        PrefabIndex = networkObject.PrefabIndex,
                        Position = networkObject.Position,
                        Rotation = networkObject.Rotation
                    };
                    _dataPackets[client.Id].Add(spawnData);
                }
                else
                {
                    var posData = new Data_Position()
                    {
                        Id = networkObject.Id,
                        Position = networkObject.Position,
                        Instantly = true
                    };
                    _dataPackets[client.Id].Add(posData);
                    var rotData = new Data_Rotation()
                    {
                        Id = networkObject.Id,
                        Rotation = networkObject.Rotation,
                        Instantly = true
                    };
                    _dataPackets[client.Id].Add(rotData);
                    var sclData = new Data_Scale()
                    {
                        Id = networkObject.Id,
                        Scale = networkObject.Scale,
                        Instantly = true
                    };
                    _dataPackets[client.Id].Add(sclData);
                }
            }
        }

        /// <summary>
        /// Send network data to all clients, except one
        /// </summary>
        /// <param name="exceptClient">Client, that shouldn`t receive data</param>
        /// <param name="data">Network data</param>
        private void SendDataExcept(ConnectedClient exceptClient, INetworkData data)
        {
            foreach (var client in _connectedClients.Values)
            {
                if (client == exceptClient)
                    continue;
                _dataPackets[client.Id].Add(data);
            }
            
        }

        /// <summary>
        /// Add data to packet for concrete client
        /// </summary>
        /// <param name="client">Network client</param>
        /// <param name="data">Network data</param>
        private void AddSendData(ConnectedClient client, INetworkData data)
        {
            _dataPackets[client.Id].Add(data);
        }

        /// <summary>
        /// Send network data packet to the client
        /// </summary>
        /// <param name="client">Network client</param>
        /// <param name="packet">Network data packet</param>
        private void SendDataPacket(ConnectedClient client, DataPacket packet)
        {
            var serializedPacket = SerializeDataPacket(packet);
            client.Connection.Send(serializedPacket);
        }

        /// <summary>
        /// Send network data packet to all connected clients
        /// </summary>
        private void SendDataPacketAll()
        {
            foreach (var client in _connectedClients.Values)
            {
                try
                {
                    if (_dataPackets[client.Id].DataList.Count > 0)
                        SendDataPacket(client, _dataPackets[client.Id]);
                }
                catch (SocketException ex)
                {
                    _logger.LogError($"Failed to send data packet to {client.PlayerName}");
                }
            }
        }

        /// <summary>
        /// Reason for client disconnection
        /// </summary>
        private enum DisconnectReason { RoomIsFull, Manual, PingFailure, SocketException}
        
        /// <summary>
        /// Close connection for the client
        /// </summary>
        /// <param name="client">Network client</param>
        /// <param name="disconnectReason">Disconnect reason</param>
        /// <exception cref="ArgumentOutOfRangeException">If disconnect reason is unknown</exception>
        private void CloseConnection(ConnectedClient client, DisconnectReason disconnectReason)
        {

            switch (disconnectReason)
            {
                case DisconnectReason.RoomIsFull:
                    _logger.LogError($"Client {client.PlayerName} tried to connect, but there`s already max players in game.");
                    break;
                case DisconnectReason.Manual:
                    _logger.Log($"Client {client.PlayerName} has manually disconnected from server.");
                    break;
                case DisconnectReason.PingFailure:
                    _logger.Log($"Client {client.PlayerName} from server due to not ping for {Constants.MAX_PING_FAILURE_COUNT} ticks.");
                    break;
                case DisconnectReason.SocketException:
                    _logger.Log($"Client {client.PlayerName} disconnected from server because of SocketException.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(disconnectReason), disconnectReason, $"Client {client.PlayerName} disconnected from server because of unknown reason.");
            }
            if (client.Connection.Connected)
            {
                _connectedClients.Remove(client.Id);
                _dataPackets.Remove(client.Id);
                client.Connection.Shutdown(SocketShutdown.Both);
                client.Connection.Close();
            }
        }

        /// <summary>
        /// Serialize data packet before sending to clients
        /// </summary>
        /// <param name="packet">Network data packet</param>
        /// <returns>Array of bytes</returns>
        private byte[] SerializeDataPacket(DataPacket packet)
        {
            byte[] bytes = MessagePackSerializer.Serialize(packet);
            return bytes;
        }

        /// <summary>
        /// Deserialize data packet
        /// </summary>
        /// <param name="serializedData">Array of bytes</param>
        /// <returns>Network DataPacket</returns>
        private DataPacket DeserializeDataPacket(byte[] serializedData)
        {
            var packet = MessagePackSerializer.Deserialize<DataPacket>(serializedData);
            if (packet.DataList.Count == 0)
                _logger.LogError("Could not recognize data.");
            return packet;
        }
        
        /// <summary>
        /// Receive new connections on listening socket
        /// </summary>
        private async void AcceptNewConnectionAsync()
        {
            Socket handler = await _listenSocket.AcceptAsync();
            var clientId = GetIdForNewClient();
            var connectedClient = new ConnectedClient(clientId, handler);
            if (clientId == -1)
            {
                CloseConnection(connectedClient, DisconnectReason.RoomIsFull);
                return;
            }
            _connectedClients[clientId] = connectedClient;
            _dataPackets[clientId] = new DataPacket();
            _logger.Log($"Accepted a new connection from {handler.RemoteEndPoint}");
        }

        /// <summary>
        /// Generate id for client
        /// </summary>
        /// <returns>Network identity</returns>
        private int GetIdForNewClient()
        {
            for (int i = 0; i < Constants.MAX_PLAYERS; i++)
            {
                if (!_connectedClients.ContainsKey(i))
                    return i;
            }
            return -1;
        }
        
        /// <summary>
        /// Loop, that handles all network server data pass
        /// </summary>
        public void Update()
        {

            ConnectedClient[] clientConnections = _connectedClients.Values.ToArray();
            Parallel.ForEach(clientConnections, (client) =>
            {
                try
                {
                    ReceiveData(client);
                    if (_dataPackets.ContainsKey(client.Id))
                    {
                        if (_dataPackets[client.Id].DataList.Count > 0)
                        {
                            SendDataPacket(client, _dataPackets[client.Id]);
                            _dataPackets[client.Id].Clear();
                        }
                    }
                }
                catch (SocketException ex)
                {
                    _logger.LogError($"Connection error: {ex.Message}");
                    CloseConnection(client, DisconnectReason.SocketException);
                }
            });
            AcceptNewConnectionAsync();
        }
            
    }
}
