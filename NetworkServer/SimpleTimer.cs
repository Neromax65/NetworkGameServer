using System;
using System.Threading;

namespace NetworkGameServer
{
    /// <summary>
    /// Custom timer with callback action
    /// </summary>
    public static class SimpleTimer
    {
        /// <summary>
        /// Start timer
        /// </summary>
        /// <param name="action">Action on timer tick end</param>
        /// <param name="period">Time in seconds</param>
        /// <param name="repeat">Repeatable timer</param>
        /// <returns></returns>
        public static Timer Start(Action action, float period, bool repeat)
        {
            return new Timer(TimerCallback, action, 0, (int)(repeat ? period * 1000 : -1));
        }

        /// <summary>
        /// Trigger callback
        /// </summary>
        /// <param name="obj">Callback action object</param>
        private static void TimerCallback(object obj)
        {
            Action action = (Action) obj;
            action?.Invoke();
        }
    }
}