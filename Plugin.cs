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
using NeuroSdk.Internal;
using System.Linq;
using NeuroSdk.Websocket;
using System.Collections;

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
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! v {MyPluginInfo.PLUGIN_VERSION}");
        InitializeHarmony();
        GenerateConfigFile();
        SetCustomHouseRules.LoadCustomRules();
        SetSettingsOptions.InitializeCustomSettings();
        NeuroSdkSetup.Initialize("For the King");
        Logger.LogMessage("Character ID: " + WebsocketConnection.Instance.Character?.CharacterId);
        Logger.LogMessage("display name: " + WebsocketConnection.Instance.Character?.DisplayName);
        // devConsole = Instantiate(new DeveloperConsole(), Instance.transform);
        // Logger.LogWarning("dev console = " + devConsole);
        // devConsole?.gameObject?.SetActive(false);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            if (!GlobalConfig.IsDebugMode()) return;
            doSpam = !doSpam;
            Logger.LogWarning("CHANGED DEBUG SPAM TO " + doSpam);
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            GlobalConfig.debugMode = !GlobalConfig.debugMode;
            Logger.LogWarning("CHANGED DEBUG MODE TO " + GlobalConfig.IsDebugMode());
        }
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Logger.LogWarning("backquote");
            // LoggerTest.Instance?.ToggleConsole();
            // devConsole.gameObject.SetActive(!devConsole.gameObject.activeSelf);
        }
    }

    bool isRunning = false;
    // no work
    IEnumerator RotateCamera()
    {
        if (isRunning) yield break;
        isRunning = true;
        float elapsedTime = 0f;
        Camera cam = FTKHub.Instance.m_OverworldCamera;
        Quaternion startRot = cam.transform.rotation;
        Vector3 addRot = new(0, 360, 0);
        Quaternion targetRot = startRot * Quaternion.Euler(addRot);
        while (elapsedTime < 1.0f)
        {
            float progress = elapsedTime / 1.0f;
            cam.transform.rotation = Quaternion.Lerp(startRot, targetRot, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cam.transform.rotation = startRot;
        isRunning = false;
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
                string json = Jason.Serialize(config);
                File.WriteAllText(configPath, json);
            }
            SetConfigValues(config);
            return;
        }
        Dictionary<string, object> _config = GlobalConfig.defaultConfig;
        string jsonString = Jason.Serialize(_config);
        File.WriteAllText(configPath, jsonString);
        config = new Dictionary<string, object>(_config);
        SetConfigValues(config);
    }

    void SetConfigValues(Dictionary<string, object> _config)
    {
        GlobalConfig.SetValues(_config);
    }
}