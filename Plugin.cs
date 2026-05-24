using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
// using NeuroSdk;
// using NeuroSdk.Actions;
using UnityEngine;

namespace NeuroFTK;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("FTK.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    readonly int test = 15;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", "ws://localhost:8000");
        // NeuroSdkSetup.Initialize("For the King");
        Test2();
    }

    [HarmonyPrefix]
    public void Test2()
    {
        Logger.LogInfo($"test: {test}");
        float new_test = 30f;
        for (int i = 0; i < 50; i++)
        {
            new_test++;
        }
        Logger.LogMessage($"test: {new_test}");
        Test3();
    }

    [HarmonyPatch(typeof(GameObject))]
    public static void Test3()
    {
        Logger.LogDebug("test3 harmony patch");
    }

    
}

