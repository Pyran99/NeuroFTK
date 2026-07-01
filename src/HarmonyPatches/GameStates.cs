using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GameStates
    {
        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.EncounterFinished))] // after dungeon battle
        [HarmonyPostfix]
        static void EncounterFinished()
        {
            Plugin.Logger.LogMessage("GameState encounter finished");
        }
        
    }
}