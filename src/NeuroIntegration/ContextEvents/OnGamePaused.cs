using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents;

[HarmonyPatch(typeof(uiOptionsMenu), nameof(uiOptionsMenu.PauseGame))]
public class OnGamePaused
{
    static void Postfix(bool ___m_Paused)
    {
        if (!uiStartGame.Instance.m_GameStarted)
        {
            return;
        }
        if (___m_Paused)
        {
            Context.Send("Game is paused", true);
        }
        else
        {
            Context.Send("Game is unpaused", true);
        }
    }
}