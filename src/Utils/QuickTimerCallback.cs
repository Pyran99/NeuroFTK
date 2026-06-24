using System;

namespace Pyran.NeuroFTK.Utils
{
    /// <summary>
    /// call a method after a certain amount of time<br/>
    /// <code>QuickTimerCallback timerCallback = new(Method, 2000f);</code>
    /// </summary>
    public class QuickTimerCallback
    {
        public QuickTimerCallback(Action method, float ms = 1000f)
        {
            Callback += method;
            Start(method, ms);
        }

        private event Action Callback;

        private void Start(Action method, float ms = 1000f)
        {
            System.Timers.Timer timer = new(ms)
            {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => Callback?.Invoke();
            timer.Elapsed += (sender, e) => Dispose(method);
            timer.Start();
        }

        private void Dispose(Action method)
        {
            Callback -= method;
        }
    }
}