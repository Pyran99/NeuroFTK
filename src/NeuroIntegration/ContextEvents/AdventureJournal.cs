using System.Collections;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
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
            uiMoreInfoMenu.Instance.StartCoroutine(ReadJournal(_messageHeader, _message, _delay));
        }

        static IEnumerator ReadJournal(string _messageHeader, string _message, float _activateDelay)
        {
            yield return new WaitForSeconds(_activateDelay + 0.1f);
            Context.Send($"[you read the journal of {_messageHeader}] {_message}"); // what other context calls this menu
            yield return new WaitForSeconds(8.0f);
            uiMoreInfoMenu.Instance.UseOkayButton();
            yield return new WaitForSeconds(uiMoreInfoMenu.Instance.m_DeactivateDelay + 0.25f);
            Encounters.EncounterMenuInstance.m_ActiveSubPanel.StartCoroutine(Encounters.DelayActions(Encounters.allButtons));
        }
    }
}