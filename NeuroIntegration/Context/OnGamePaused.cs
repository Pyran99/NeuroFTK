using HarmonyLib;

namespace NeuroFTK.NeuroIntegration.Context;

[HarmonyPatch(typeof(uiOptionsMenu), nameof(uiOptionsMenu.PauseGame))]
public class OnGamePaused
{
    static void Postfix(bool pause)
    {
        Plugin.Logger.LogInfo($"Paused: {pause}");
        //TODO send context to neuro
    }
}