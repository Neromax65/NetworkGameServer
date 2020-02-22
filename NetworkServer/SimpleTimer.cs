using System;
using System.Threading;

namespace NetworkGameServer
{
    public static class SimpleTimer
    {
        public static Timer Start(Action action, float period, bool repeat)
        {
            return new Timer(TimerCallback, action, 0, (int)(repeat ? period * 1000 : -1));
        }

        private static void TimerCallback(object obj)
        {
            Action action = (Action) obj;
            action?.Invoke();
        }
    }
}