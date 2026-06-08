using NeuroSdk.Actions;
using UnityEngine;

namespace Pyran.NeuroFTK
{
    /// <summary>
    /// This is mainly for making actions yourself without using the ActionWindow where you action would only disable the object holding the ActionWindow. (press button that only hides window without an unregister call instead of sending an action)
    /// for objects that get disabled instead of deleted this will call Destroy on the ActionWindow when this is disabled (owners SetActive(false)).
    /// <para>This component is checked for before adding and can be destroyed to</para>
    /// </summary>
    public class UnregisterDisabledObject: MonoBehaviour
    {
        public static UnregisterDisabledObject QuickCreate(GameObject owner, ActionWindow window, bool destroySelf = true)
        {
            UnregisterDisabledObject comp = owner.GetComponent<UnregisterDisabledObject>() ?? owner.AddComponent<UnregisterDisabledObject>();
            comp.window = window;
            return comp;
        }

        public ActionWindow window;
        public bool destroySelf = true;

        private void OnDisable()
        {
            Debug.Log("disabled object destroy window");
            Destroy(window);
            if (destroySelf)
            {
                Destroy(this);
            }
        }
    }
}