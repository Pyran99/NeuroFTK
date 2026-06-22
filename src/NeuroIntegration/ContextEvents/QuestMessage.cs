using System.Collections;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents
{
    [HarmonyPatch(typeof(uiQuestConfirmHud), nameof(uiQuestConfirmHud.InitializeMessage))]
    public class QuestMessage
    {
        static void Postfix(uiQuestConfirmHud __instance)
        {
            //quest confirm hud: Go to <color=#FBB060>Woodsmoke</color> in <color=#FBB060>The Guardian Forest</color>
            string msg = StringReplace.RemoveStyling(__instance.m_Message.text);
            Context.Send("[quest objective] " + msg);
            __instance.StartCoroutine(Close(__instance));
        }

        static IEnumerator Close(uiQuestConfirmHud instance)
        {
            yield return new WaitForSeconds(3f);
            instance.UseOkayButton();
        }
    }
}