using System.Collections;
using System.Text;
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

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.InitiateEncounterSessionRPC))] // main battle enter for normal & dungeon, also called for each dungeon encounter, including ready actions
        [HarmonyPostfix]
        // static void EnteredBattle(MiniHexDungeon.EncounterType _encounterType)
        static void EnteredBattle()
        {
            Plugin.Logger.LogMessage("StartEncounterSession");
            Battle.isCombatEncounter = false;
            ToggleDisposableActions.ToggleOverworldActions(false);
            MiniHexDungeon.EncounterType _encounterType = EncounterSessionMC.Instance.GetCurrentEncounter().EncounterType;
            switch (_encounterType)
            {
                case MiniHexDungeon.EncounterType.Next:
                case MiniHexDungeon.EncounterType.Ready:
                case MiniHexDungeon.EncounterType.Stair:
                case MiniHexDungeon.EncounterType.EmptyRoom:
                case MiniHexDungeon.EncounterType.Door:
                    Plugin.Logger.LogWarning($"encounter type = {_encounterType}");
                    Context.Send($"{BeginTurns.GetSimplifiedTeamState()}", true);
                    // CharacterDecisionButtons.AddItemUse(true);
                    break;
            }
        }

        // [HarmonyPatch(typeof(GameFlow), nameof(GameFlow.LocalInitCombatSession))] // for overworld battles only
        // [HarmonyPostfix]
        // static void EnteredCombat()
        // {
        //     // StartEncounterSession
        //     // EncounterSessionMC.InitiateEncounterSessionRPC
        //     Plugin.Logger.LogWarning("GameFlow initiate combat"); // calls EncounterSessionMC enter
        // }
        
        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceStair))]
        [HarmonyPostfix]
        static void DungeonStairs()
        {
            Context.Send($"entered a dungeon room with stairs to the next floor. [team state] {BeginTurns.CtxCombatTurnBeginPlayer(CharacterData.GetActiveCow())}");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceEmptyRoom))]
        [HarmonyPostfix]
        static void EmptyRoom()
        {
            Context.Send($"entered an empty room", true);
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
            Context.Send("battle finished, returning to overworld", true);
            OverworldFlow.isFirstAction = false;
            ToggleDisposableActions.ToggleCombatActions(false);
            CameraUtils.Zoom(100f);
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.EncounterFinished))] // after normal battle & dungeon complete
        [HarmonyPostfix]
        static void EncounterFinished()
        {
            Plugin.Logger.LogMessage("CombatEvents encounter finished");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), "ReturnToOverworld")]
        [HarmonyPatch]
        static void ReturnToOverworld()
        {
            Plugin.Logger.LogWarning("NYI ReturnToOverworld (no idea when called, not from normal battles)");
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
            Plugin.Logger.LogWarning($"acid destroy {__result}");
            Context.Send($"acid destroyed {ItemData.GetItemName(__result)} from {CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)}");
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.StealEquippedItem))] // acid also calls this
        [HarmonyPostfix]
        static void ItemStolenEquipped(FTK_itembase.ID _equippedItem, CharacterStats __instance)
        {
            Plugin.Logger.LogWarning($"item stolen {_equippedItem}");
            if (isAcidDestroy)
            {
                Plugin.Logger.LogWarning($"acid steal item {_equippedItem}");
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

        static StringBuilder stolenSb = new();
        static bool stealDelay = false;

        static void ItemStolenCtx(FTK_itembase.ID _item, CharacterOverworld cow)
        {
            stolenSb.AppendLine($"{ItemData.GetItemName(_item)} stolen from {CharacterData.GetCharacterName(cow)}");
            if (stealDelay) return;
            stealDelay = true;
            Plugin.Instance.StartCoroutine(StealDelay());
        }

        static IEnumerator StealDelay()
        {
            yield return null;
            Context.Send(stolenSb.ToString());
            stolenSb = new();
            stealDelay = false;
        }

        [HarmonyPatch(typeof(EnemyDummy), nameof(EnemyDummy.AddStolen))]
        [HarmonyPostfix]
        static void StolenItem(FTK_itembase.ID _item, int _gold, EnemyDummy __instance)
        {
            string enemyName = CombatUtils.GetEnemyName(__instance);
            string itemStolen = "";
            if (_item != FTK_itembase.ID.None) itemStolen = ItemData.GetItemName(_item);
            StringBuilder sb = new($"{enemyName} stole");
            if (itemStolen != "") sb.Append($" {itemStolen},");
            if (_gold > 0) sb.Append($" {_gold} gold");
            Context.Send(sb.ToString());
        }

        [HarmonyPatch(typeof(CharacterEventListener), nameof(CharacterEventListener.WeaponBreak))]
        [HarmonyPrefix]
        static void WeaponBreak(CharacterEventListener __instance)
        {
            if (!__instance.m_IsWeapBreakOn) return;
            Context.Send($"{CharacterData.GetCharacterName(__instance.m_CharacterOverworld)}'s weapon broke");
        }

        [HarmonyPatch(typeof(EncounterChest), nameof(EncounterChest.DisplayStaticMessage))]
        [HarmonyPrefix]
        static void ChestIdentified(MiniHexDungeon.EncounterType _displayAsType, bool _identified)
        {
            if (_identified)
            {
                switch (_displayAsType)
                {
                    case MiniHexDungeon.EncounterType.UnlockedChestMimic:
                    case MiniHexDungeon.EncounterType.LockedChestMimic:
                        Context.Send($"this is a mimic chest and will start combat if opened");
                        break;
                }
            }
        }

    }
}