using UnityEngine;

namespace NetworkGameServer
{
    /// <summary>
    /// Class represent object that should be shared through network
    /// </summary>
    public class NetworkObject
    {
        /// <summary>
        /// Network identity of object
        /// </summary>
        public int Id { get; private set; }
        
        /// <summary>
        /// Index of Unity prefab, that represent this object
        /// </summary>
        public int PrefabIndex { get; private set; }
        
        /// <summary>
        /// Player network identity, that owns this object 
        /// </summary>
        public int OwningPlayerId { get; private set; }
        
        /// <summary>
        /// Network position of the object
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// Network rotation of the object
        /// </summary>
        public Quaternion Rotation { get; set; }
        
        /// <summary>
        /// Network scale of the object
        /// </summary>
        public Vector3 Scale { get; set; }
        
        public NetworkObject(int id, int prefabIndex, int owningPlayerId)
        {
            Id = id;
            PrefabIndex = prefabIndex;
            OwningPlayerId = owningPlayerId;
            Position = new Vector3(0,0,0);
            Rotation = new Quaternion(0,0,0,0);
            Scale = new Vector3(1, 1, 1);
        }
    }
}