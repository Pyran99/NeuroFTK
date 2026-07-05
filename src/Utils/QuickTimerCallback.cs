using System;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    /// <summary>
    /// call a method after a certain amount of time<br/>
    /// <code>QuickTimerCallback timerCallback = new(Method, owner, 2000f);</code>
    /// </summary>
    public class QuickTimerCallback
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="owner">`null` will always invoke method</param>
        /// <param name="ms"></param>
        public QuickTimerCallback(Action method, GameObject owner, float ms = 1000f)
        {
            Callback += method;
            Start(method, owner, ms);
        }

        public event Action Callback;

        private void Start(Action method, GameObject owner, float ms = 1000f)
        {
            System.Timers.Timer timer = new(ms)
            {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => Finished(method, owner);
            timer.Start();
        }

        private void Finished(Action method, GameObject owner)
        {
            if (owner == null) Callback?.Invoke();
            else if (owner.activeInHierarchy) Callback?.Invoke();
            Dispose(method);
        }

        private void Dispose(Action method)
        {
            Callback -= method;
        }
    }
}