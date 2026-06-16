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

namespace Pyran.NeuroFTK;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("FTK.exe")]
[HarmonyPatch]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    readonly string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NeuroFTKConfig.json");
    public static Dictionary<string, object> config = [];
    public static Plugin Instance { get; private set; }
    /// <summary>
    /// toggle message spam from update related calls
    /// </summary>
    public static bool doSpam = false;


    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        InitializeHarmony();
        GenerateConfigFile();
        SetCustomHouseRules.LoadCustomRules();
        SetSettingsOptions.InitializeCustomSettings();
        NeuroSdkSetup.Initialize("For the King");
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            if (!GlobalConfig.debug_mode) return;
            doSpam = !doSpam;
        }
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
            bool keyAdded = false;
            foreach (KeyValuePair<string, object> entry in GlobalConfig.defaultConfig)
            {
                if (config.ContainsKey(entry.Key)) continue;
                config.Add(entry.Key, entry.Value);
                keyAdded = true;
            }
            if (keyAdded)
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            SetConfigValues(config);
            return;
        }
        Dictionary<string, object> _config = GlobalConfig.defaultConfig;
        string jsonString = JsonConvert.SerializeObject(_config, Formatting.Indented);
        File.WriteAllText(configPath, jsonString);
        config = new Dictionary<string, object>(_config);
        SetConfigValues(config);
    }

    void SetConfigValues(Dictionary<string, object> _config)
    {
        CustomHouseRules.SET_CUSTOM_RULES = (bool)_config["use_custom_rules"];
        GlobalConfig.debug_mode = (bool)_config["debug_mode"];
        Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", (string)_config["environment_web_socket"]);
        Logger.LogWarning($"debug_mode is {GlobalConfig.debug_mode}");
    }
}