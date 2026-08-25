using System.Collections;
using FTKHelp;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GameEndScreen
    {
        public static bool isCreditsFinished = false;

        [HarmonyPatch(typeof(CreditScreen), nameof(CreditScreen.ShowEndGame))]
        [HarmonyPostfix]
        static void OnGameFinished()
        {
            Plugin.Logger.LogWarning("ShowEndGame");
            Context.Send("the credits are rolling");
            // QuickTimerCallback timer = new(CreditScreen.Instance.OnCloseButton, CreditScreen.Instance.gameObject, 30000f);
        }

        [HarmonyPatch(typeof(CreditScreen), nameof(CreditScreen.OnClose))]
        [HarmonyPrefix]
        static void Test1()
        {
            Plugin.Logger.LogWarning("verify => credits OnClose");
        }

        [HarmonyPatch(typeof(CreditScreen), nameof(CreditScreen.OnCloseButton))]
        [HarmonyPrefix]
        static void Test2()
        {
            // from screen click
        }

        //FIXME time scale is set to 0 after credits
        // float end = Time.realtimeSinceStartup + 5f;

        [HarmonyPatch(typeof(CreditScreen), "Update")]
        [HarmonyPostfix]
        static void OnCreditsUpdate()
        {
            if (CreditScreen.Instance.m_EndReached && !isCreditsFinished) // not called if manually skipped
            {
                isCreditsFinished = true;
                Plugin.Logger.LogWarning("credits finished");
                // QuickTimerCallback timer = new(CreditScreen.Instance.OnCloseButton, CreditScreen.Instance.gameObject, 7000f);
            }
        }

        [HarmonyPatch(typeof(GameEventManager), "ShowStoneHero_CR")] // may not be called in other adventures
        [HarmonyPostfix]
        static IEnumerator ShowingStoneHero(IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
            Plugin.Logger.LogWarning("ShowStoneHero_CR finished");
            Plugin.Logger.LogWarning("timescale = " + Time.timeScale);
            // CreditScreen.Instance.OnCloseButton(); // works for instant close

            // SelectButton.StartUnityBtnCoroutine(CreditScreen.Instance.transform.Find("Button").GetComponent<Button>(), 5f);
        }
    }
}