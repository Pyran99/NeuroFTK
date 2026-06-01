using System.Collections;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace NeuroFTK.HarmonyPatches.AutomatedActions;

// skips the intro splash screen & prepare to die popup
[HarmonyPatch]
public class SkipGameIntro
{
    static bool firstLoad = true;

    [HarmonyPatch(typeof(SplashScreen), "Start")]
    [HarmonyPostfix]
    static void AfterSplashScreenStart()
    {
        Context.Send("For the King game context");
    }

    // DisplayScene uses an input key press to skip, always return true
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
        Plugin.Logger.LogMessage("game start");
        if (firstLoad)
        {
            firstLoad = false;
            __instance.StartCoroutine(Wait());
        }
    }

    static IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.25f);
        Plugin.Logger.LogMessage("skipping difficulty warning");
        uiStartGame.Instance.OnPrepareToDie();
    }
}
