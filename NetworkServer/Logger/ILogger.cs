namespace NetworkGameServer.Logger
{
    public interface ILogger
    {
        // void Log(string message);
        // void Log(int message);
        // void Log(float message);
        // void Log(bool message);
        // void Log(byte message);
        void Log(object message);
        void LogError(object message);
    }
}