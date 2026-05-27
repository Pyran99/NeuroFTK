using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NeuroFTK.GameConfigs;
using NeuroFTK.HarmonyPatches.AutomatedActions;
using Newtonsoft.Json;

namespace NeuroFTK;
/*A mod for Neuro to play 'For the King'
Customizable data can be found in CustomHouseRules.json & NeuroFTKConfig.json , located in the same directory as NeuroFTK.dll
*/

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
        var harmony = new Harmony("Pyran."+MyPluginInfo.PLUGIN_GUID+".ForTheKing");
        harmony.PatchAll();
        GenerateConfigFile();
        SetCustomHouseRules.LoadCustomRules();
        Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", (string)config["environmentWebSocket"]);
        // NeuroSdkSetup.Initialize("For the King");
    }

    private void GenerateConfigFile()
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

    // [HarmonyPatch(typeof(uiFTKButton), nameof(uiFTKButton.OnPointerEnter))]
    // [HarmonyPrefix]
    // private static void TestMod()
    // {
    //     Logger.LogMessage("POINTER ENTERED TEST");
    //         AudioManager.Instance.MainMenuButtonClick();
    //         Logger.LogMessage("default click");
    // }

}
