using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

        public static IEnumerator UseFocus(int count, uiFTKButton btn, CharacterStats stats)
        {
            btn.OnPointerEnter(null);
            yield return null;
            while (stats.m_FocusPoints > 0 && count > 0)
            {
                count--;
                btn.m_OnRightClick?.Invoke();
                yield return new WaitForSeconds(VisualParams.Instance.FocusAnimTime + 0.01f);
                yield return null;
            }
            StartCoroutine(btn, 1.0f);
        }

        public static void StartCoroutine(uiFTKButton button, float wait = 1.0f)
        {
            button.StartCoroutine(SelectButtonWithDelay(button, wait));
        }

        static IEnumerator SelectButtonWithDelay(uiFTKButton button, float wait = 1.0f)
        {
            if (button == null)
            {
                Plugin.Logger.LogError($"button is null");
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

        public static void StartUnityBtnCoroutine(Button button, float wait = 1.0f)
        {
            button.StartCoroutine(SelectUnityButtonWithDelay(button, wait));
        }

        static IEnumerator SelectUnityButtonWithDelay(Button button, float wait = 1.0f)
        {
            if (button == null)
            {
                Plugin.Logger.LogError($"button is null");
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
            button.OnSubmit(null);
        }
    
        
    }
}