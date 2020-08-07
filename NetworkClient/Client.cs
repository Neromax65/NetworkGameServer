using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using NetworkGameServer;
using NetworkGameServer.Logger;
using NetworkGameServer.NetworkData;

namespace NetworkClient
{
    /// <summary>
    /// Network client
    /// FIXME: Obsolete
    /// </summary>
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
            
            
            _clientLoopTimer = SimpleTimer.Start(ClientLoop, Constants.TIME_BETWEEN_TICK, true);
        }

        private async void ClientLoop()
        {
            try
            {
                if (_serverConnection == null || !_serverConnection.Connected)
                {
                    CloseConnection();
                    return;
                }
                ReceiveData();
                // var ping = new Data_Ping();
                // await SendDataAsync(ping);
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
            byte[] bytes = MessagePackSerializer.Serialize(data);
            // var test = MessagePackSerializer.Deserialize<INetworkData>(bytes);
            // _logger.Log($"TEST DATA {test}:");
            return bytes;
            // byte[] buffer = new byte[Constants.BUFFER_SIZE];
            // BinaryFormatter formatter = new BinaryFormatter();
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
            // INetworkData data;
            // using (var stream = new MemoryStream(serializedData))
            // {
            //     data = (INetworkData)formatter.Deserialize(stream);
            // }
            // return data;
        }

        private bool IsAnyDataReceived()
        {
            if (_serverConnection.Available > 0) return true;
            
            // _pingFailureCount++;
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
            List<INetworkData> dataList = DeserializeData(buffer, bytes);

            foreach (var data in dataList)
            {
                HandleCommand(data.Command, data);
            }
        }
        
        private void HandleCommand(byte command, INetworkData data)
        {
            switch (command)
            {
                case Command.None:
                    _logger.Log("No command was received.");
                    break;
                case Command.Ping:
                    break;
                case Command.Position:
                    var moveData = data as Data_Position;
                    // _logger.Log($"Received move data: (GameObject Id: {moveData.Id} X:{moveData.X}, Y:{moveData.Y}, Z:{moveData.Z})");
                    break;
                default:
                    _logger.Log("Unrecognized command.");
                    break;
            }
        }

        private void CloseConnection()
        {
            _logger.Log("Disconnecting...");
            if (_serverConnection == null)
                return;
            _clientLoopTimer.Dispose();
            _serverConnection.Shutdown(SocketShutdown.Both);
            _serverConnection.Close();
            _serverConnection = null;
        }
        
    }
}