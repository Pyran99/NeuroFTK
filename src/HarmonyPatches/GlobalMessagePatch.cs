using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using NeuroSdk.Actions;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GlobalMessagePatch
    {
        static ActionWindow window;

        [HarmonyPatch(typeof(uiGlobalMessageHUD), "ActivateMessagePanel")]
        [HarmonyPostfix]
        static IEnumerator PanelShown(IEnumerator __result, uiGlobalMessageHUD __instance)
        {
            while (__result.MoveNext()) yield return __result.Current;
            __instance.StartCoroutine(QuickTimerCallback.WaitRoutine(() => GetButtons(__instance), __instance.m_MessagePanel.gameObject));
        }

        [HarmonyPatch(typeof(uiGlobalMessageHUD), nameof(uiGlobalMessageHUD.UseOkayButton))]
        [HarmonyPrefix]
        static void MessageClosedOkay()
        {
            WindowClosed();
        }

        [HarmonyPatch(typeof(uiGlobalMessageHUD), nameof(uiGlobalMessageHUD.UseYesButton))]
        [HarmonyPrefix]
        static void MessageClosedYes()
        {
            WindowClosed();
        }

        [HarmonyPatch(typeof(uiGlobalMessageHUD), nameof(uiGlobalMessageHUD.UseNoButton))]
        [HarmonyPrefix]
        static void MessageClosedNo()
        {
            WindowClosed();
        }

        static void WindowClosed()
        {
            Object.Destroy(window);
        }

        static void GetButtons(uiGlobalMessageHUD _instance)
        {
            Dictionary<string, object> buttons = [];
            if (_instance.m_ChoiceButtonPanel.gameObject.activeSelf)
            {
                buttons.Add("yes", _instance.m_YesButton.GetComponent<uiFTKButton>());
                buttons.Add("no", _instance.m_NoButton.GetComponent<uiFTKButton>());
            }
            else
            {
                buttons.Add("continue", _instance.m_ClickAnywhere);
            }
            window = GlobalMessageAction.RegisterAction(_instance, new(buttons));
        }
    }
}