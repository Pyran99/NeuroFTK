using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NeuroFTK;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("FTK.exe")]
[HarmonyPatch]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        var harmony = new Harmony("Pyran."+MyPluginInfo.PLUGIN_GUID+".ForTheKing");
        harmony.PatchAll();
        // Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", "ws://localhost:8000");
        // NeuroSdkSetup.Initialize("For the King");
    }


    // [HarmonyPatch(typeof(uiFTKButton), nameof(uiFTKButton.OnPointerEnter))]
    // [HarmonyPrefix]
    // private static void TestMod(uiFTKButton __instance)
    // {
    //     print($"print {__instance}");
    //     Debug.Log($"debug log {__instance}");
    //     Logger.LogMessage("POINTER ENTERED TEST");
    //     Logger.LogMessage($"sound = {__instance.m_ClickSound}");
    //     if (__instance.m_ClickSound != null && __instance.m_ClickSound.m_EventID != 0)
    //     {
    //         AudioManager.Instance.AudioEvent(__instance.m_ClickSound.m_EventID);
    //         Logger.LogMessage($"event id = {__instance.m_ClickSound.m_EventID}");
    //     }
    //     else
    //     {
    //         AudioManager.Instance.MainMenuButtonClick();
    //         Logger.LogMessage("default click");
    //     }
    // }

    // [HarmonyPatch(typeof(uiFTKButton), nameof(uiFTKButton.OnPointerEnter))]
    // [HarmonyPrefix]
    // private static bool TestCoroutine(out IEnumerator __result)
    // {
    //     __result = Coroutine();
    //     return false;
        
    //     static IEnumerator Coroutine()
    //     {
    //         Logger.LogMessage("BEFORE POINTER ENTERED TEST");
    //         yield return new WaitForSeconds(1f);
    //         Logger.LogMessage("WAITED POINTER ENTERED TEST");
    //         yield break;
    //     }

    // }
}
