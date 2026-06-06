using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pyran.NeuroFTK
{
    public class SelectButton
    {
        public static void StartCoroutine(MonoBehaviour instance, uiFTKButton button, float wait = 1.0f)
        {
            instance.StartCoroutine(SelectButtonWithDelay(button, wait));
        }

        static IEnumerator SelectButtonWithDelay(uiFTKButton button, float wait = 1.0f)
        {
            if (button == null) yield break;
            if (!button.isActiveAndEnabled)
            {
                Plugin.Logger.LogWarning($"button {button.name} is disabled");
                yield break;
            }
            button.OnPointerEnter(null);
            yield return new WaitForSeconds(wait);
            Assert.IsNotNull(button);
            button?.OnControllerClick();
        }
    
        
    }
}