using MessagePack;

namespace NetworkGameServer.NetworkData
{
    [MessagePackObject]
    public struct Command
    {
        public const byte None = 0;
        public const byte Ping = 1;
        public const byte Position = 2;
        public const byte Rotation = 3;
        public const byte Scale = 4;
    }
}