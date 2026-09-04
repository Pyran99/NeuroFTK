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

        public static void StartCoroutine(uiFTKButton button, float wait = 1.0f)
        {
            button.StartCoroutine(SelectButtonWithDelay(button, wait));
        }

        public static void StartCoroutineWithFocus(uiFTKButton button, int focusCount, CharacterStats stats, float wait = 1.0f)
        {
            button.StartCoroutine(SelectButtonWithDelay(button, wait, focusCount, stats));
        }

        static IEnumerator SelectButtonWithDelay(uiFTKButton button, float wait = 1.0f, int focusCount = 0, CharacterStats stats = null)
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
            // if (EventSystem.current.currentSelectedGameObject != button.gameObject)
            // {
            // }
            button.OnPointerEnter(null);
            yield return new WaitForSeconds(wait);
            if (focusCount > 0 && stats != null)
            {
                while (stats.m_FocusPoints > 0 && focusCount > 0)
                {
                    Plugin.Logger.LogWarning("focus used");
                    focusCount--;
                    button.m_OnRightClick?.Invoke();
                    yield return new WaitForSeconds(VisualParams.Instance.FocusAnimTime + 0.025f);
                    yield return null;
                }
            }
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