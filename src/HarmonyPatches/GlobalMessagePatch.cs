using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GlobalMessagePatch
    {

        [HarmonyPatch(typeof(uiGlobalMessageHUD), "ActivateMessagePanel")]
        [HarmonyPostfix]
        static IEnumerator PanelShown(IEnumerator __result, uiGlobalMessageHUD __instance)
        {
            while (__result.MoveNext()) yield return __result.Current;
            QuickTimerCallback timer = new(() => GetButtons(__instance), 1000f);
        }

        static void GetButtons(uiGlobalMessageHUD _instance)
        {
            Dictionary<string, object> buttons = [];
            if (_instance.m_ClickAnywhere.isActiveAndEnabled)
            {
                buttons.Add("continue", _instance.m_ClickAnywhere);
            } 
            else
            {
                buttons.Add("yes", _instance.m_YesButton.GetComponent<uiFTKButton>());
                buttons.Add("no", _instance.m_NoButton.GetComponent<uiFTKButton>());
            }
            GlobalMessageAction.RegisterAction(_instance, new(buttons));
        }
    }
}