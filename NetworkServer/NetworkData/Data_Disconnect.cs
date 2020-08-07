﻿﻿using MessagePack;

namespace NetworkGameServer.NetworkData
{
    /// <summary>
    /// Disconnection data class
    /// </summary>
    [MessagePackObject]
    public class Data_Disconnect : Data_Base
    {
        public Data_Disconnect()
        {
            Command = NetworkData.Command.Disconnect;
        }
    }
}