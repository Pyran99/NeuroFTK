using System.Collections;
using FTKHelp;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GameEndScreen
    {
        public enum EndType
        {
            StoneHero,
            DungeonRun,
        }

        [HarmonyPatch(typeof(CreditScreen), nameof(CreditScreen.ShowEndGame))]
        [HarmonyPostfix]
        static void OnGameFinished()
        {
            Context.Send("the credits are rolling");
        }

        [HarmonyPatch(typeof(CreditScreen), nameof(CreditScreen.OnCloseButton))]
        [HarmonyPrefix]
        static void CloseScreen()
        {
            // from screen click
        }

        [HarmonyPatch(typeof(GameEventManager), "ShowStoneHero_CR")] // may not be called in other adventures
        [HarmonyPostfix]
        static IEnumerator ShowingStoneHero(IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
            Context.Send($"the credits are finished, you have completed your adventure!");
            ActionWindow window = ActionWindow.Create(CreditScreen.Instance.gameObject);
            window.AddAction(new EndScreenAction(EndType.StoneHero));
            window.SetForce(15, "finish the adventure", "", true);
            UnregisterDisabledObject.QuickCreate(CreditScreen.Instance.gameObject, window);
        }

        public static void SelectButton()
        {
            if (!CreditScreen.Instance.gameObject.activeInHierarchy) return;
            CreditScreen.Instance.transform.Find("Button").GetComponent<Button>().OnSubmit(null);
        }

        // [HarmonyPatch(typeof(CreditScreen), "Update")]
        // [HarmonyPostfix]
        // static void OnCreditsUpdate()
        // {
        //     if (CreditScreen.Instance.m_EndReached && !isCreditsFinished) // not called if manually skipped
        //     {
        //         isCreditsFinished = true;
        //         Plugin.Logger.LogMessage("credits finished");
        //         QuickTimerCallback timer2 = new(true, CreditScreen.Instance.OnCloseButton, CreditScreen.Instance.gameObject, 7000f);
        //     }
        // }
    }
}