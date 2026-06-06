using NeuroSdk.Actions;
using UnityEngine;

namespace Pyran.NeuroFTK
{
    /// <summary>
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
            Destroy(window);
            if (destroySelf)
            {
                Destroy(this);
            }
        }
    }
}