using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

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
    // private static void TestMod()
    // {
    //     Logger.LogMessage("POINTER ENTERED TEST");
//         AudioManager.Instance.MainMenuButtonClick();
//         Logger.LogMessage("default click");
    // }

}
