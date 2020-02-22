using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private async Task AcceptNewConnectionAsync()
        {
            Socket handler = await _listenSocket.AcceptAsync();
            _clientConnections.Add(handler);
            _pingFailureCountDict[handler] = 0;
            _logger.Log($"Accepted a new connection from {handler.RemoteEndPoint}");
        }

        private bool IsAnyDataReceived(Socket clientConnection)
        {
            if (clientConnection.Available > 0) return true;
            
            _pingFailureCountDict[clientConnection]++;
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

            _pingFailureCountDict[clientConnection] = 0;

            byte[] buffer = new byte[Constants.BUFFER_SIZE];
            int bytes = await clientConnection.ReceiveAsync(buffer, SocketFlags.None);
            INetworkData data = DeserializeData(buffer);

            var key = data.Command;

            switch (key)
            {
                case Command.None:
                    _logger.Log("No command was received.");
                    break;
                case Command.Ping:
                    _logger.Log($"Received ping from client ({clientConnection.RemoteEndPoint})");
                    break;
                case Command.Move:
                    var moveData = data as Data_Move;
                    _logger.Log($"Received move data: ({moveData.X},{moveData.Y},{moveData.Z})");
                    break;
                default:
                    _logger.Log("Unrecognized command.");
                    break;
            }
            var pingData = new Data_Ping();
            SendDataTo(clientConnection, pingData);
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
        
        private void SendDataAll(INetworkData data)
        {
            _clientConnections.ForEach(cc => SendDataTo(cc, data));
        }


        private void SendDataTo(Socket clientConnection, INetworkData data)
        {
            var serializedData = SerializeData(data);
            clientConnection.Send(serializedData);
        }
        
        private async void SendDataToAsync(Socket clientConnection, INetworkData data)
        {
            var serializedData = SerializeData(data);
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
            byte[] buffer = new byte[Constants.BUFFER_SIZE];
            BinaryFormatter formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(buffer))
            {
                formatter.Serialize(stream, data);
            }
            return buffer;
        }
        
        private INetworkData DeserializeData(byte[] serializedData)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            INetworkData data;
            using (var stream = new MemoryStream(serializedData))
            {
                data = (INetworkData)formatter.Deserialize(stream);
            }
            return data;
        }
        

        private async void ServerLoop()
        {
            try
            {
                Socket[] clientConnections = _clientConnections.ToArray();
                foreach (var clientConnection in clientConnections)
                {
                    if (!clientConnection.Connected)
                    {
                        CloseConnection(clientConnection);
                        continue;
                    }

                    ReceiveData(clientConnection);
                }

                await AcceptNewConnectionAsync();
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
