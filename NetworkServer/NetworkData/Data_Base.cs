
namespace NetworkGameServer.NetworkData
{
    [System.Serializable]
    public abstract class Data_Base : INetworkData
    {
        public byte Command { get; set; }
    }
}