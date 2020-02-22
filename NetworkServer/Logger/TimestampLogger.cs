using System;

namespace NetworkGameServer.Logger
{
    public class TimestampLogger : ILogger
    {
        public void Log(object message)
        {
            PrintWithTimeStamp(message.ToString());
        }

        public void LogError(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            PrintWithTimeStamp(message.ToString());
            Console.ResetColor();
        }

        private void PrintWithTimeStamp(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {message}");
        }
    }
}