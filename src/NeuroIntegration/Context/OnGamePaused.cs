using HarmonyLib;

namespace Pyran.NeuroFTK.NeuroIntegration.Context;

// [HarmonyPatch(typeof(uiOptionsMenu), nameof(uiOptionsMenu.PauseGame))]
public class OnGamePaused
{
    static void Postfix(bool ___m_Paused)
    {
        Plugin.Logger.LogInfo($"Paused: {___m_Paused}");
        //TODO send context to neuro
    }
}