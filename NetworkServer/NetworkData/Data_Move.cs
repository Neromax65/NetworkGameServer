namespace NetworkGameServer.NetworkData
{
    [System.Serializable]
    public class Data_Move : Data_Base
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        
        public Data_Move(float x, float y, float z)
        {
            Command = NetworkData.Command.Move;
            X = x;
            Y = y;
            Z = z;
        }
    }
}