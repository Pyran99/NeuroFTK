using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;
using Newtonsoft.Json;
using NeuroSdk;
using UnityEngine;
using NeuroSdk.Actions;

namespace Pyran.NeuroFTK;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("FTK.exe")]
[HarmonyPatch]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    readonly string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NeuroFTKConfig.json");
    public static Dictionary<string, object> config = [];

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        InitializeHarmony();
        GenerateConfigFile();
        SetCustomHouseRules.LoadCustomRules();
        SetSettingsOptions.InitializeCustomSettings();
        Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", (string)config["environmentWebSocket"]);
        NeuroSdkSetup.Initialize("For the King");
    }

    void InitializeHarmony()
    {
        string id = "Pyran." + MyPluginInfo.PLUGIN_GUID + ".ForTheKing";
        var harmony = new Harmony(id);
        harmony.PatchAll();
        Logger.LogInfo($"Harmony patch applied {id}");
    }

    void GenerateConfigFile()
    {
        if (File.Exists(configPath))
        {
            config = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(configPath));
            // CustomHouseRules.SET_CUSTOM_RULES = bool.TryParse((string)config["useCustomRules"], out bool result) && result;
            CustomHouseRules.SET_CUSTOM_RULES = (bool)config["useCustomRules"];
            return;
        }

        Dictionary<string, dynamic> _config = new()
        {
            { "environmentWebSocket", "ws://localhost:8000" },
            { "useCustomRules", CustomHouseRules.SET_CUSTOM_RULES },
        };
        string jsonString = JsonConvert.SerializeObject(_config, Formatting.Indented);
        File.WriteAllText(configPath, jsonString);
        config = new Dictionary<string, object>(_config);
    }
}