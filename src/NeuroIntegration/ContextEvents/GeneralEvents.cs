using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GeneralEvents
    {
        [HarmonyPatch(typeof(MiniHexBoat), nameof(MiniHexBoat.RepairBoat))]
        [HarmonyPostfix]
        static void OnBoatRepaired(MiniHexBoat __instance)
        {
            Context.Send($"boat repaired at {HexData.GetVec2Pos(__instance.m_HexLand)}", true);
        }
        
    }
}