using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GameStates
    {
        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.EncounterFinished))]
        [HarmonyPostfix]
        static void EncounterFinished()
        {
            Context.Send("encounter finished");
        }
        
    }
}