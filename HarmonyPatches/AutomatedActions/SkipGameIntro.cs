using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace NeuroFTK.HarmonyPatches.AutomatedActions
{
    /*
    skips the intro splash screen & prepare to die popup
    */
    [HarmonyPatch]
    public class SkipGameIntro
    {
        [HarmonyPatch(typeof(SplashScreen), "Start")]
        [HarmonyPostfix]
        static void AfterSplashScreenStart()
        {
            //TODO can send game context to neuro
            Plugin.Logger.LogMessage("Send game context to Neuro");
        }

        // DisplayScene uses a button press to skip, always return true
        [HarmonyPatch(typeof(SplashScreen), "GetAnyButton")]
        [HarmonyPrefix]
        static bool SkipSplashScreen(ref bool __result)
        {
            __result = true;
            Plugin.Logger.LogMessage("skipping splash screen");
            return false;
        }

        [HarmonyPatch(typeof(GameStart), "Start")]
        [HarmonyPostfix]
        static void AfterStart(GameStart __instance)
        {
            Plugin.Logger.LogMessage("main menu shown");
            __instance.StartCoroutine(Wait());
        }

        static IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.25f);
            Plugin.Logger.LogMessage("skipping difficulty warning");
            uiStartGame.Instance.OnPrepareToDie();
        }
    }
}