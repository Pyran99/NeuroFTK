using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;

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

        [HarmonyPatch(typeof(DungeonScroller), nameof(DungeonScroller.DungeonExit))]
        [HarmonyPostfix]
        static void DungeonExit()
        {
            Plugin.Logger.LogMessage("dungeon exit");
            Context.Send("battle finished, returning to overworld", true); // is called from normal battle
            OverworldFlow.isFirstAction = false;
            ToggleDisposableActions.ToggleOverworldActions(true);
            ToggleDisposableActions.ToggleCombatActions(false);
            CameraUtils.Zoom(100f);
            // FTKHub.Instance.m_OverworldCamera.GetComponent<RtsCamera>().Rotation = 0f;
        }

        [HarmonyPatch(typeof(EncounterSessionMC), "ReturnToOverworld")] // dungeon only maybe? not called from normal battle
        [HarmonyPatch]
        static void ReturnToOverworld()
        {
            Plugin.Logger.LogMessage("NYI returning to overworld");
            Context.Send("battle finished, returning to overworld", true);
            CameraUtils.Zoom(100f);
            // FTKHub.Instance.m_OverworldCamera.GetComponent<RtsCamera>().Rotation = 0f;
        }

        // to next room, from popup menu
        [HarmonyPatch(typeof(uiExploreDungeonMenu), nameof(uiExploreDungeonMenu.OnExplore))]
        [HarmonyPrefix]
        static void Test3()
        {
            Context.Send("moving to next room", true);
        }

    }
}