using System;
using System.Collections;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    /// <summary>
    /// call a method after a certain amount of time (in ms)<br/>
    /// <code>QuickTimerCallback timerCallback = new(Method, owner, 2000f);</code>
    /// </summary>
    public class QuickTimerCallback
    {
        /// <param name="owner">`null` will always invoke method</param>
        public QuickTimerCallback(Action method, GameObject owner, float ms = 1000f)
        {
            Callback += method;
            Start(method, owner, ms);
        }

        /// <param name="owner">`null` will always invoke method</param>
        /// <param name="ignorePause">ignores timeScale</param>
        public QuickTimerCallback(bool ignorePause, Action method, GameObject owner, float ms = 1000f)
        {
            Callback += method;
            Start(method, owner, ms, ignorePause);
        }

        public event Action Callback;

        private void Start(Action method, GameObject owner, float ms = 1000f, bool ignorePause = false)
        {
            System.Timers.Timer timer = new(ms)
            {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => Finished(method, owner, ignorePause);
            timer.Start();
        }

        private void Finished(Action method, GameObject owner, bool ignorePause = false)
        {
            if (Mathf.Approximately(Time.timeScale, 0f)) Plugin.Instance.StartCoroutine(Wait(method, owner, ignorePause));
            else DoCall(method, owner);
        }

        private void Dispose(Action method)
        {
            Callback -= method;
        }

        private IEnumerator Wait(Action method, GameObject owner, bool ignorePause = false)
        {
            while (!ignorePause && Mathf.Approximately(Time.timeScale, 0f))
            {
                yield return null;
            }
            DoCall(method, owner);
        }

        private void DoCall(Action method, GameObject owner)
        {
            if (owner == null) Callback?.Invoke();
            else if (owner.activeInHierarchy) Callback?.Invoke();
            Dispose(method);
        }
    }
}