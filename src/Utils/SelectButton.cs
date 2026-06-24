using System.Collections;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    /// <summary>
    /// Utility class to select a button, then wait before clicking
    /// </summary>
    public class SelectButton
    {
        public static void InstantClick(uiFTKButton button)
        {
            if (button == null)
            {
                Plugin.Logger.LogError($"button is null");
                return;
            }
            if (!button.isActiveAndEnabled)
            {
                Plugin.Logger.LogWarning($"button {button.name} is disabled");
                return;
            }
            button.OnPointerEnter(null);
            button.Select();
            button.OnControllerClick();
        }

        public static void StartCoroutine(MonoBehaviour instance, uiFTKButton button, float wait = 1.0f)
        {
            instance.StartCoroutine(SelectButtonWithDelay(instance, button, wait));
        }

        static IEnumerator SelectButtonWithDelay(MonoBehaviour instance, uiFTKButton button, float wait = 1.0f)
        {
            if (button == null)
            {
                Plugin.Logger.LogError($"button is null from {instance}");
                yield break;
            }
            if (!button.isActiveAndEnabled)
            {
                Plugin.Logger.LogWarning($"button {button.name} is disabled");
                yield break;
            }
            button.OnPointerEnter(null);
            yield return new WaitForSeconds(wait);
            if (!button.isActiveAndEnabled)
            {
                Plugin.Logger.LogError($"button is disabled after waiting {button.name}");
                yield break;
            }
            button.OnControllerClick();
        }
    
        
    }
}