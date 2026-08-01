using System.Collections;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class AdventureJournal
    {
        [HarmonyPatch(typeof(uiMoreInfoMenu), nameof(uiMoreInfoMenu.InitializeMessage))]
        [HarmonyPostfix]
        static void ShowMessage(string _messageHeader, string _message, float _delay, uiMoreInfoMenu __instance)
        {
            Context.Send($"[you read the journal of {_messageHeader}] {_message}"); // what other context calls this menu
            QuickTimerCallback timer = new(() =>
            {
                uiMoreInfoMenu.Instance.StartCoroutine(Wait());
            }, uiLocationMenuDisplay.Instance.gameObject, 2.5f);
        }

        static IEnumerator Wait()
        {
            uiMoreInfoMenu.Instance.UseOkayButton();
            yield return new WaitForSeconds(uiMoreInfoMenu.Instance.m_DeactivateDelay + 0.1f);
            EncounterLocation.CreateActionWindow();
        }
    }
}