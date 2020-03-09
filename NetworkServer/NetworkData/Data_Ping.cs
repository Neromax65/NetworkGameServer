﻿using MessagePack;

 namespace NetworkGameServer.NetworkData
{
    [MessagePackObject]
    public class Data_Ping : Data_Base
    {
        public Data_Ping()
        {
            Command = NetworkData.Command.Ping;
        }
    }
}