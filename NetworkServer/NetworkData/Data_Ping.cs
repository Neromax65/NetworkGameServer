namespace NetworkGameServer.NetworkData
{
    [System.Serializable]
    public class Data_Ping : Data_Base
    {
        public Data_Ping()
        {
            Command = NetworkData.Command.Ping;
        }
    }
}