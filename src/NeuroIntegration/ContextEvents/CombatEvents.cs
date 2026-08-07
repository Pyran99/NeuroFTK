using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CombatEvents
    {
        static bool isAcidDestroy = false;
        
        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceStair))]
        [HarmonyPostfix]
        static void DungeonStairs()
        {
            Context.Send("entered a dungeon room with stairs to the next floor. ", true);
            BeginTurns.CtxCombatTurnBeginPlayer(CharacterData.GetNeuroCow(true));
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

        [HarmonyPatch(typeof(DungeonScroller), nameof(DungeonScroller.DungeonExit))] // is called from normal battle
        [HarmonyPostfix]
        static void DungeonExit()
        {
            Context.Send("battle finished, returning to overworld", true);
            OverworldFlow.isFirstAction = false;
            ToggleDisposableActions.ToggleCombatActions(false);
            CameraUtils.Zoom(100f);
        }

        [HarmonyPatch(typeof(EncounterSessionMC), "ReturnToOverworld")]
        [HarmonyPatch]
        static void ReturnToOverworld()
        {
            Plugin.Logger.LogMessage("NYI ReturnToOverworld (no idea when called)");
            // Context.Send("battle finished, returning to overworld", true);
            // CameraUtils.Zoom(100f);
        }

        // to next room, from popup menu
        [HarmonyPatch(typeof(uiExploreDungeonMenu), nameof(uiExploreDungeonMenu.OnExplore))]
        [HarmonyPrefix]
        static void Test3()
        {
            Context.Send("moving to next room", true);
        }

        [HarmonyPatch(typeof(ProficiencyStealBase), "DestroyRandomEquippedItem")]
        [HarmonyPrefix]
        static void ItemDestroyed(CharacterDummy _dummy, ref FTK_itembase.ID __result)
        {
            if (_dummy is EnemyDummy) return;
            isAcidDestroy = true;
            Context.Send($"acid destroyed {ItemData.GetItemName(__result)} from {CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)}");
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.StealEquippedItem))] // acid also calls this
        [HarmonyPostfix]
        static void ItemStolenEquipped(FTK_itembase.ID _equippedItem, CharacterStats __instance)
        {
            if (isAcidDestroy)
            {
                isAcidDestroy = false;
                return;
            }
            ItemStolenCtx(_equippedItem, __instance.m_CharacterOverworld);
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.StealBeltItem))]
        [HarmonyPostfix]
        static void ItemStolenBelt(FTK_itembase.ID _beltItem, CharacterStats __instance)
        {
            ItemStolenCtx(_beltItem, __instance.m_CharacterOverworld);
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.StealPackItem))]
        [HarmonyPostfix]
        static void ItemStolenPack(FTK_itembase.ID _packItem, CharacterStats __instance)
        {
            ItemStolenCtx(_packItem, __instance.m_CharacterOverworld);
        }

        static void ItemStolenCtx(FTK_itembase.ID _item, CharacterOverworld cow)
        {
            Context.Send($"{ItemData.GetItemName(_item)} stolen from {CharacterData.GetCharacterName(cow)}");
        }

    }
}