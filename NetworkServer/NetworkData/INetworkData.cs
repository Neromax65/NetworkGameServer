using MessagePack;

namespace NetworkGameServer.NetworkData
{
    [Union(0, typeof(Data_Base))]
    [Union(1, typeof(Data_Ping))]
    [Union(2, typeof(Data_Position))]
    [Union(3, typeof(Data_Rotation))]
    [Union(4, typeof(Data_Scale))]
    public interface INetworkData
    {
        [Key(0)] byte Command { get; set; }
    }
}