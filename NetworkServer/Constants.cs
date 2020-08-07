namespace NetworkGameServer
{
    /// <summary>
    /// Server constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Maximum number of server ticks for not receiving any NetworkData
        /// </summary>
        public const int MAX_PING_FAILURE_COUNT = 50;
        
        /// <summary>
        /// Maximum number of queued connections
        /// </summary>
        public const int MAX_PENDING_CONNECTIONS = 10;
        
        /// <summary>
        /// Maximum number of player ingame
        /// </summary>
        public const int MAX_PLAYERS = 10;
        
        /// <summary>
        /// Buffer size for one unit of NetworkData
        /// </summary>
        public const int BUFFER_SIZE = 1024;
        
        /// <summary>
        /// Time between server loop ticks
        /// </summary>
        public const float TIME_BETWEEN_TICK = 0.02f;
    }
}