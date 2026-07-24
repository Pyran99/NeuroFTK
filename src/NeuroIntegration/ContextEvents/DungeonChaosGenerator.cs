using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class DungeonChaosGenerator
    {
        [HarmonyPatch(typeof(ChaosGeneratorDungeonEncounter), nameof(ChaosGeneratorDungeonEncounter.AttemptSuccess))]
        [HarmonyPostfix]
        static void AttemptSuccess() => Context.Send(StringMessages.CultDeviceDestroyed);

        [HarmonyPatch(typeof(ChaosGeneratorDungeonEncounter), nameof(ChaosGeneratorDungeonEncounter.AttemptFail))]
        [HarmonyPostfix]
        static void AttemptFail() => Context.Send(StringMessages.CultDeviceDestroyedFail);
    }
}