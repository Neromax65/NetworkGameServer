using System;

namespace NetworkGameServer.Logger
{
    /// <summary>
    /// Custom class of console logger, that prints timestamps and red messages for errors
    /// </summary>
    public class TimestampLogger : ILogger
    {
        /// <inheritdoc cref="ILogger"/>
        public void Log(object message)
        {
            PrintWithTimeStamp(message.ToString());
        }
        
        /// <inheritdoc cref="ILogger"/>
        public void LogError(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            PrintWithTimeStamp(message.ToString());
            Console.ResetColor();
        }

        /// <summary>
        /// Print message to console with timestamp
        /// </summary>
        /// <param name="message">Message to print</param>
        private void PrintWithTimeStamp(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {message}");
        }
    }
}