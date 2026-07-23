using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GameStates
    {
        public static uiGameTrackerHUD.GameTrackerMode mode;

        // changes during battle loot screen
        [HarmonyPatch(typeof(uiGameTrackerHUD), nameof(uiGameTrackerHUD.ToggleGameTrackerMode))]
        [HarmonyPostfix]
        static void GameModeChanged(uiGameTrackerHUD.GameTrackerMode _mode)
        {
            mode = _mode;
            Plugin.Logger.LogMessage($"game track mode changed to {_mode}");
        }


        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.EncounterFinished))] // after dungeon battle
        [HarmonyPostfix]
        static void EncounterFinished()
        {
            Plugin.Logger.LogMessage("GameState encounter finished");
        }
        
    }
}