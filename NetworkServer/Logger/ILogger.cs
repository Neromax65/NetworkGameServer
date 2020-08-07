namespace NetworkGameServer.Logger
{
    /// <summary>
    /// Logger interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Print message
        /// </summary>
        /// <param name="message">Message to print</param>
        void Log(object message);
        
        /// <summary>
        /// Print error message
        /// </summary>
        /// <param name="message">Message to print</param>
        void LogError(object message);
    }
}