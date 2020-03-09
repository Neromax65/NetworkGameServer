using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using NetworkGameServer.Logger;
using NetworkGameServer.NetworkData;

namespace NetworkGameServer
{
    public class Server
    {
        private Socket _listenSocket;
        // private List<Socket> _clientConnections;

        // private Dictionary<Socket, int> _pingFailureCountDict;
        private Dictionary<int, ConnectedClient> _connectedClients;
        private Dictionary<int, NetworkObject> _networkObjects;
        
        
        private Timer _serverLoopTimer;
        private ILogger _logger;
        
        public void Start(string ip, int port)
        {
            _logger = new TimestampLogger();
            _connectedClients = new Dictionary<int, ConnectedClient>();
            _networkObjects = new Dictionary<int, NetworkObject>();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(ipEndPoint);
            _logger.Log($"Server is started on {ip}:{port}");
            // _clientConnections = new List<Socket>();
            
            // _pingFailureCountDict = new Dictionary<Socket, int>();
            _listenSocket.Listen(Constants.MAX_PENDING_CONNECTIONS);
            _logger.Log($"Listening to connections...");
            _serverLoopTimer = SimpleTimer.Start(ServerLoop, Constants.TIME_BETWEEN_TICK, true);
            Console.ReadKey();
        }



        private bool IsAnyDataReceived(Socket clientConnection)
        {
            if (clientConnection.Available > 0) return true;
            
            // _pingFailureCountDict[clientConnection]++;
            // _logger.Log($"Ping failure count: {_pingFailureCountDict[clientConnection]}");
            // if (_pingFailureCountDict[clientConnection] >= Constants.MAX_PING_FAILURE_COUNT)
            // {
            //     _logger.Log($"Disconnecting client {clientConnection.RemoteEndPoint} due to not ping for {Constants.MAX_PING_FAILURE_COUNT} ticks.");
            //     clientConnection.Disconnect(false);
            // }
            return false;
        }

        private async void ReceiveData(ConnectedClient client)
        {
            if (!IsAnyDataReceived(client.Connection))
                return;
            // _pingFailureCountDict[clientConnection] = 0;

            byte[] buffer = new byte[Constants.BUFFER_SIZE];
            int bytes = await client.Connection.ReceiveAsync(buffer, SocketFlags.None);
            List<INetworkData> dataList = DeserializeData(buffer, bytes);
            if (dataList == null || dataList.Count == 0)
                return;
            // Parallel.ForEach(dataList, data =>
            // {
            //     HandleCommand(data.Command, data, clientConnection);
            // });
            foreach (var data in dataList)
            {
                HandleCommand(data.Command, data, client);
            }

        }

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
                    _networkObjects[posData.Id].Position = new Vector3(posData.X, posData.Y, posData.Z);
                    SendDataExcept(client, data);
                    break;
                case Command.Rotation:
                    var rotData = data as Data_Rotation;
                    _networkObjects[rotData.Id].Rotation = new Vector4(rotData.X, rotData.Y, rotData.Z, rotData.W);
                    SendDataExcept(client, data);
                    break;
                case Command.Scale:
                    var sclData = data as Data_Scale;
                    _networkObjects[sclData.Id].Scale = new Vector3(sclData.X, sclData.Y, sclData.Z);
                    SendDataExcept(client, data);
                    break;
                case Command.Spawn:
                    SendDataExcept(client, data);
                    break;
                case Command.Connect:
                    var connectData = data as Data_Connect;
                    client.PlayerName = connectData.PlayerName;
                    _logger.Log($"Player {client.PlayerName} connected.");
                    Synchronize(client);
                    break;
                case Command.Disconnect:
                    CloseConnection(client);
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


        private void Synchronize(ConnectedClient client)
        {
            foreach (var networkObject in _networkObjects.Values)
            {
                if (networkObject.PrefabIndex != -1)
                {
                    var spawnData = new Data_Spawn()
                    {
                        PrefabIndex = networkObject.PrefabIndex,
                        PosX = networkObject.Position.X,
                        PosY = networkObject.Position.Y,
                        PosZ = networkObject.Position.Z,
                        RotX = networkObject.Rotation.X,
                        RotY = networkObject.Rotation.Y,
                        RotZ = networkObject.Rotation.Z,
                        RotW = networkObject.Rotation.W
                    };
                    SendDataTo(client, spawnData);
                }
                else
                {
                    var posData = new Data_Position()
                    {
                        Id = networkObject.Id,
                        X = networkObject.Position.X,
                        Y = networkObject.Position.Y,
                        Z = networkObject.Position.Z
                    };
                    SendDataTo(client, posData);
                    var rotData = new Data_Rotation()
                    {
                        Id = networkObject.Id,
                        X = networkObject.Rotation.X,
                        Y = networkObject.Rotation.Y,
                        Z = networkObject.Rotation.Z,
                        W = networkObject.Rotation.W
                    };
                    SendDataTo(client, rotData);
                    var sclData = new Data_Scale()
                    {
                        Id = networkObject.Id,
                        X = networkObject.Scale.X,
                        Y = networkObject.Scale.Y,
                        Z = networkObject.Scale.Z
                    };
                    SendDataTo(client, sclData);
                }
            }
        }

        private void SendDataExcept(ConnectedClient exceptClient, INetworkData data)
        {
            foreach (var connectedClient in _connectedClients.Values)
            {
                if (connectedClient == exceptClient)
                    continue;
                SendDataTo(connectedClient, data);
            }
            
        }
        
        private async void SendDataExceptAsync(ConnectedClient exceptClient, INetworkData data)
        {
            foreach (var connectedClient in _connectedClients.Values)
            {
                if (connectedClient == exceptClient)
                    continue;
                SendDataToAsync(connectedClient, data);
            }
            
        }
        
        private void SendDataAll(INetworkData data)
        {
            _connectedClients.Values.ToList().ForEach(cc => SendDataTo(cc, data));
        }


        private void SendDataTo(ConnectedClient client, INetworkData data)
        {
            var serializedData = SerializeData(data);
            // if (data is Data_Move dataMove)
            // {
            //     _logger.Log($"Data: (GameObjectId: {dataMove.Id}, X:{dataMove.X}, Y:{dataMove.Y}, Z:{dataMove.Z}) was sent to client {clientConnection.RemoteEndPoint}");
            // }
            try
            {
                client.Connection.Send(serializedData);    
            }
            catch (SocketException ex)
            {
                _logger.LogError($"Error while sending data to client: {ex.Message}");
            }
        }
        
        private async void SendDataToAsync(ConnectedClient client, INetworkData data)
        {
            var serializedData = SerializeData(data);
            // if (data is Data_Position)
            //     _logger.Log($"Data: {data} was sent to client {clientConnection.RemoteEndPoint}");
            await client.Connection.SendAsync(serializedData, SocketFlags.None);
        }

        private void CloseConnection(ConnectedClient client)
        {

            _logger.Log($"Client {client.PlayerName} has disconnected from server.");
            _connectedClients.Remove(client.Id);
            // _pingFailureCountDict.Remove(clientConnection);
            client.Connection.Shutdown(SocketShutdown.Both);
            client.Connection.Close();
        }


        private byte[] SerializeData(INetworkData data)
        {

            byte[] bytes = MessagePackSerializer.Serialize(data);
            return bytes;
        }
        
        private List<INetworkData> DeserializeData(byte[] serializedData, int totalDataLength)
        {
            try
            {
                List<INetworkData> dataList = new List<INetworkData>();
                int bytesRead = 0;
                // _logger.Log($"Deserializing data of total length: {totalDataLength}");
                do
                {
                    // _logger.Log($"Bytes read: {bytesRead}");
                    INetworkData data = MessagePackSerializer.Deserialize<INetworkData>(serializedData, out var curBytesRead);
                    dataList.Add(data);
                    // _logger.Log($"DataList length: {dataList.Count}");
                    bytesRead += curBytesRead;
                    serializedData = serializedData.Skip(curBytesRead).ToArray();
                } while (bytesRead < totalDataLength);
                return dataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        
        private async void AcceptNewConnectionAsync()
        {
            Socket handler = await _listenSocket.AcceptAsync();
            // _clientConnections.Add(handler);
            var clientId = GetIdForNewClient();
            var connectedClient = new ConnectedClient(clientId, handler);
            if (clientId == -1)
            {
                CloseConnection(connectedClient);
                return;
            }
            _connectedClients[clientId] = connectedClient;
            // _pingFailureCountDict[handler] = 0;
            _logger.Log($"Accepted a new connection from {handler.RemoteEndPoint}");
        }

        private int GetIdForNewClient()
        {
            for (int i = 0; i < Constants.MAX_PLAYERS; i++)
            {
                if (!_connectedClients.ContainsKey(i))
                    return i;
            }

            _logger.LogError($"Client tried to connect, but there`s already max players in game.");
            return -1;
        }
        
        private async void ServerLoop()
        {
            try
            {
                // Socket[] clientConnections = _clientConnections.ToArray();
                ConnectedClient[] clientConnections = _connectedClients.Values.ToArray();
                Parallel.ForEach(clientConnections, (client) =>
                {
                    if (client.Connection.Connected)
                        ReceiveData(client);
                    else 
                        CloseConnection(client);
                });

                AcceptNewConnectionAsync();
            }
            catch (SocketException ex)
            {
                _logger.LogError($"Connection error: {ex.Message}");
                // TODO: Disconnect client?
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }
            
    }
}
