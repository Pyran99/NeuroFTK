using System;
using BepInEx;
using BepInEx.Logging;
using NeuroSdk;
using NeuroSdk.Actions;

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
        test2(null);
        Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", "ws://localhost:8000");
        NeuroSdkSetup.Initialize("For the King");
        
    }

    public void test2(ActionWindow window)
    {
        Logger.LogInfo($"test: {test}");
        float new_test = 30f;
        for (int i = 0; i < 50; i++)
        {
            new_test++;
        }
        Logger.LogMessage($"test: {new_test}");
    }

    public class TestClass
    {
        private int test;
        public TestClass(int test)
        {
            this.test = test;
        }
    }
    
}

