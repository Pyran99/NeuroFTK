using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CombatEncounters
    {
        
        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceStair))]
        [HarmonyPostfix]
        static void DungeonStairs()
        {
            Context.Send("entered a dungeon room with stairs to the next floor", true);
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceEmptyRoom))]
        [HarmonyPostfix]
        static void EmptyRoom()
        {
            Context.Send("entered an empty room", true);
        }

        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.ShowDungeonShopRPC))]
        [HarmonyPostfix]
        static void DungeonShop()
        {
            Context.Send("entered a shop room", true);
        }

    }
}