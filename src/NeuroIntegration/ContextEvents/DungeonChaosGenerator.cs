using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class DungeonChaosGenerator
    {
        [HarmonyPatch(typeof(ChaosGeneratorDungeonEncounter), nameof(ChaosGeneratorDungeonEncounter.AttemptSuccess))]
        [HarmonyPostfix]
        static void AttemptSuccess() => Context.Send("you successfully destroyed the chaos generator");

        [HarmonyPatch(typeof(ChaosGeneratorDungeonEncounter), nameof(ChaosGeneratorDungeonEncounter.AttemptFail))]
        [HarmonyPostfix]
        static void AttemptFail() => Context.Send("you failed to destroy the chaos generator");
    }
}