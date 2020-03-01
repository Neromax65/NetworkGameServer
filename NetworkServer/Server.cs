using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private List<Socket> _clientConnections;

        private Dictionary<Socket, int> _pingFailureCountDict;
        private Timer _serverLoopTimer;
        private ILogger _logger;
        
        public void Start(string ip, int port)
        {
            _logger = new TimestampLogger();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(ipEndPoint);
            _logger.Log($"Server is started on {ip}:{port}");
            _clientConnections = new List<Socket>();
            _pingFailureCountDict = new Dictionary<Socket, int>();
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
            if (_pingFailureCountDict[clientConnection] >= Constants.MAX_PING_FAILURE_COUNT)
            {
                _logger.Log($"Disconnecting client {clientConnection.RemoteEndPoint} due to not ping for {Constants.MAX_PING_FAILURE_COUNT} ticks.");
                clientConnection.Disconnect(false);
            }
            return false;
        }

        private async void ReceiveData(Socket clientConnection)
        {
            if (IsAnyDataReceived(clientConnection))
                _pingFailureCountDict[clientConnection] = 0;
            else
                return;

            byte[] buffer = new byte[Constants.BUFFER_SIZE];
            int bytes = await clientConnection.ReceiveAsync(buffer, SocketFlags.None);
            List<INetworkData> dataList = DeserializeData(buffer, bytes);
            if (dataList == null || dataList.Count == 0)
                return;
            // Parallel.ForEach(dataList, data =>
            // {
            //     HandleCommand(data.Command, data, clientConnection);
            // });
            foreach (var data in dataList)
            {
                HandleCommand(data.Command, data, clientConnection);
            }

        }

        private void HandleCommand(byte command, INetworkData data, Socket clientConnection)
        {
            switch (command)
            {
                case Command.None:
                    _logger.Log("No command was received.");
                    break;
                case Command.Ping:
                    // _logger.Log($"Received ping from client ({clientConnection.RemoteEndPoint})");
                    // var pingData = new Data_Ping();
                    // SendDataTo(clientConnection, pingData);
                    break;
                case Command.Position:
                    // var moveData = data as Data_Position;
                    // _logger.Log($"Received move data: (GameObject Id: {moveData.Id} X:{moveData.X}, Y:{moveData.Y}, Z:{moveData.Z})");
                    SendDataExcept(clientConnection, data);
                    // pingData = new Data_Ping();
                    // SendDataTo(clientConnection, pingData);
                    break;
                case Command.Rotation:
                    SendDataExcept(clientConnection, data);
                    break;
                case Command.Scale:
                    SendDataExcept(clientConnection, data);
                    break;
                default:
                    _logger.Log("Unrecognized command.");
                    break;
            }
        }

        private void SendDataExcept(Socket exceptClientConnection, INetworkData data)
        {
            foreach (var clientConnection in _clientConnections)
            {
                if (clientConnection == exceptClientConnection)
                    continue;
                SendDataTo(clientConnection, data);
            }
            
        }
        
        private async void SendDataExceptAsync(Socket exceptClientConnection, INetworkData data)
        {
            foreach (var clientConnection in _clientConnections)
            {
                if (clientConnection == exceptClientConnection)
                    continue;
                SendDataToAsync(clientConnection, data);
            }
            
        }
        
        private void SendDataAll(INetworkData data)
        {
            _clientConnections.ForEach(cc => SendDataTo(cc, data));
        }


        private void SendDataTo(Socket clientConnection, INetworkData data)
        {
            var serializedData = SerializeData(data);
            // if (data is Data_Move dataMove)
            // {
            //     _logger.Log($"Data: (GameObjectId: {dataMove.Id}, X:{dataMove.X}, Y:{dataMove.Y}, Z:{dataMove.Z}) was sent to client {clientConnection.RemoteEndPoint}");
            // }
            try
            {
                clientConnection.Send(serializedData);    
            }
            catch (SocketException ex)
            {
                _logger.LogError($"Error while sending data to client: {ex.Message}");
            }
        }
        
        private async void SendDataToAsync(Socket clientConnection, INetworkData data)
        {
            var serializedData = SerializeData(data);
            if (data is Data_Position)
                _logger.Log($"Data: {data} was sent to client {clientConnection.RemoteEndPoint}");
            await clientConnection.SendAsync(serializedData, SocketFlags.None);
        }

        private void CloseConnection(Socket clientConnection)
        {

            _logger.Log($"Client {clientConnection.RemoteEndPoint} has disconnected from server.");
            _clientConnections.Remove(clientConnection);
            _pingFailureCountDict.Remove(clientConnection);
            clientConnection.Shutdown(SocketShutdown.Both);
            clientConnection.Close();
        }


        private byte[] SerializeData(INetworkData data)
        {

            byte[] bytes = MessagePackSerializer.Serialize(data);
            // var test = MessagePackSerializer.Deserialize<INetworkData>(bytes);
            // _logger.Log($"TEST DATA {test}:");
            return bytes;
            // byte[] buffer = new byte[Constants.BUFFER_SIZE];
            // BinaryFormatter formatter = new BinaryFormatter();
            // formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
            // using (var stream = new MemoryStream(buffer))
            // {
            //     formatter.Serialize(stream, data);
            // }
            // return buffer;
        }
        
        private List<INetworkData> DeserializeData(byte[] serializedData, int totalDataLength)
        {
            try
            {
                List<INetworkData> dataList = new List<INetworkData>();
                int bytesRead = 0;
                _logger.Log($"Deserializing data of total length: {totalDataLength}");
                do
                {
                    _logger.Log($"Bytes read: {bytesRead}");
                    INetworkData data = MessagePackSerializer.Deserialize<INetworkData>(serializedData, out var curBytesRead);
                    dataList.Add(data);
                    _logger.Log($"DataList length: {dataList.Count}");
                    bytesRead += curBytesRead;
                    serializedData = serializedData.Skip(curBytesRead).ToArray();
                    _logger.Log($"Left to deserialize: {totalDataLength - bytesRead}");
                } while (bytesRead < totalDataLength);
                return dataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
            // BinaryFormatter formatter = new BinaryFormatter();
            // formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
            // INetworkData data;
            // using (var stream = new MemoryStream(serializedData))
            // {
            //     data = (INetworkData)formatter.Deserialize(stream);
            // }
            // return data;
        }
        
        private async void AcceptNewConnectionAsync()
        {
            Socket handler = await _listenSocket.AcceptAsync();
            _clientConnections.Add(handler);
            _pingFailureCountDict[handler] = 0;
            _logger.Log($"Accepted a new connection from {handler.RemoteEndPoint}");
        }
        
        private async void ServerLoop()
        {
            try
            {
                Socket[] clientConnections = _clientConnections.ToArray();
                Parallel.ForEach(clientConnections, (clientConnection) =>
                {
                    if (clientConnection.Connected)
                        ReceiveData(clientConnection);
                    else 
                        CloseConnection(clientConnection);
                });
                // foreach (var clientConnection in clientConnections)
                // {
                //     if (!clientConnection.Connected)
                //     {
                //         CloseConnection(clientConnection);
                //         continue;
                //     }
                //
                //     ReceiveData(clientConnection);
                // }

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
