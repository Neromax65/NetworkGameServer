using System.Numerics;

namespace NetworkGameServer
{
    public class NetworkObject
    {
        public int Id { get; private set; }
        public int PrefabIndex { get; private set; }
        public int OwningPlayerId { get; private set; }
        
        public Vector3 Position { get; set; }
        public Vector4 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        
        public NetworkObject(int id, int prefabIndex, int owningPlayerId)
        {
            Id = id;
            PrefabIndex = prefabIndex;
            OwningPlayerId = owningPlayerId;
            Position = Vector3.Zero;
            Rotation = Vector4.Zero;
            Scale = Vector3.One;
        }
    }
}