using System.Collections;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches;

// skips the intro splash screen & prepare to die popup
[HarmonyPatch]
public class SkipGameIntro
{
    static bool firstLoad = true;
    const string GAME_DESCRIPTION = "The King is dead, murdered by an unknown assailant. Now the once peaceful kingdom of Fahrul is in chaos. With nowhere left to turn and stretched beyond her means, the queen has put out a desperate plea to the citizens of the land to rise-up and help stem the tide of impending doom. Will you brave the relentless elements, fight the wicked creatures, sail the seas and delve into the dark underworld? None before you have returned from their journey. Can you be the one to put an end to the Chaos? For The King is a challenging blend of Strategy, JRPG Combat, and Roguelike elements.";
    const string PREPARE_TO_DIE_MSG = "Warning: Do not set out on this quest with the expectation that you will succeed at first try. Your betters have gone before you, and fallen to the last. Yet do set out, and strive. Learn what you can. And when you fail and your light is extinguished forever, despair not, for many more answer the call each day.";

    [HarmonyPatch(typeof(SplashScreen), "Start")]
    [HarmonyPostfix]
    static void AfterSplashScreenStart()
    {
        Context.Send($"you are playing 'For the King'. {GAME_DESCRIPTION}");
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
        if (firstLoad)
        {
            firstLoad = false;
            __instance.StartCoroutine(Wait());
        }
    }

    static IEnumerator Wait()
    {
        Context.Send(PREPARE_TO_DIE_MSG);
        StringBuilder sb = new();
        sb.AppendLine($"Config set:");
        foreach (KeyValuePair<string, object> kvp in Plugin.config)
        {
            sb.AppendLine($"{kvp.Key}: {kvp.Value}");
        }
        if (GlobalConfig.MaxHexSearch <= 0)
        {
            Plugin.Logger.LogError($"invalid max_hex_search value {GlobalConfig.MaxHexSearch}. value is reset to 50");
            GlobalConfig.ResetMaxSearch();
        }
        Plugin.Logger.LogWarning(sb.ToString());
        Plugin.Logger.LogMessage("Character ID: " + WebsocketConnection.Instance.Character?.CharacterId);
        Plugin.Logger.LogMessage("display name: " + WebsocketConnection.Instance.Character?.DisplayName);
        yield return new WaitForSeconds(2f);
        Plugin.Logger.LogMessage("skipping difficulty warning");
        uiStartGame.Instance.OnPrepareToDie();
    }
}
