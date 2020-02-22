using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using NetworkGameServer;
using NetworkGameServer.Logger;
using NetworkGameServer.NetworkData;

namespace NetworkClient
{
    public class Client
    {
        private Socket _serverConnection;

        private int _pingFailureCount;
        private Timer _clientLoopTimer;
        private ILogger _logger;
        
        public void Connect(string ip, int port)
        {
            _logger = new TimestampLogger();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _serverConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
            _logger.Log($"Trying to connect to {ip}:{port}...");
            _serverConnection.Connect(ipEndPoint);
            _logger.Log($"Successfully connected to {ip}:{port}");
            
            
            // TODO: Make this work
            // AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Disconnect();
            
            _clientLoopTimer = SimpleTimer.Start(ClientLoop, Constants.TIME_BETWEEN_TICK, true);
        }

        private async void ClientLoop()
        {
            try
            {
                if (!_serverConnection.Connected)
                {
                    CloseConnection();
                    return;
                }
                ReceiveData();
                var ping = new Data_Ping();
                await SendDataAsync(ping);
            }
            catch (SocketException ex)
            {
                _logger.LogError($"Connection error: {ex.Message}");
                // TODO: Disconnect from server?
                CloseConnection();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public void SendData(INetworkData data)
        {
            byte[] serializedData = SerializeData(data);
            _serverConnection.Send(serializedData);
        }
        
        public async Task SendDataAsync(INetworkData data)
        {
            byte[] serializedData = SerializeData(data);
            await _serverConnection.SendAsync(serializedData, SocketFlags.None);
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

        private bool IsAnyDataReceived()
        {
            if (_serverConnection.Available > 0) return true;
            
            _pingFailureCount++;
            if (_pingFailureCount >= Constants.MAX_PING_FAILURE_COUNT)
            {
                _logger.Log($"Disconnecting from server due to not ping for {Constants.MAX_PING_FAILURE_COUNT} ticks.");
                _serverConnection.Disconnect(false);
            }
            return false;
        }
        
        private async void ReceiveData()
        {
            if (IsAnyDataReceived())
                _pingFailureCount = 0;
            else
                return;
                
            byte[] buffer = new byte[Constants.BUFFER_SIZE];
            var bytes = await _serverConnection.ReceiveAsync(buffer, SocketFlags.None);
            INetworkData data = DeserializeData(buffer);

            var key = data.Command;

            switch (key)
            {
                case Command.None:
                    _logger.Log("No command was received.");
                    break;
                case Command.Ping:
                    _logger.Log("Received ping from server.");
                    break;
                case Command.Move:
                    break;
                default:
                    _logger.Log("Unrecognized command.");
                    break;
            }
        }

        private void CloseConnection()
        {
            _logger.Log("Disconnecting...");
            _clientLoopTimer.Dispose();
            _serverConnection.Shutdown(SocketShutdown.Both);
            _serverConnection.Close();
            _serverConnection = null;
        }
        
    }
}