using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    /// <summary>
    /// add patches to track when they occur
    /// </summary>
    [HarmonyPatch]
    public class Tests
    {
        [HarmonyPatch(typeof(HexLand), nameof(HexLand.TogglePing))]
        [HarmonyPostfix]
        static void Ping(HexLand __instance)
        {
            if (GlobalConfig.debug_mode == false) return;
            Plugin.Logger.LogMessage("ping data");
            StringBuilder sb = new();
            sb.AppendLine($"\nid: {__instance.GetHexLandID().m_BigIndex} - {__instance.GetHexLandID().m_SmallIndex}");
            sb.AppendLine($"pos: {__instance.GetPosition()}");
            sb.AppendLine($"realm: {__instance.GetRealm()}"); // GuardianForest
            sb.AppendLine($"boat: {__instance.IsBoat()}");
            sb.AppendLine($"loc display: {__instance.GetLocationDisplayValue(GameLogic.Instance.GetCurrentCOW())}"); // The Guardian Forest, is realm display if not dungeon
            sb.AppendLine($"distance: {Math.Round(HexLand.Distance(GameLogic.Instance.GetCurrentCOW().m_HexLand, __instance), 2)}");
            // _ = HexLand.FindPath(GameLogic.Instance.GetCurrentCOW().m_HexLand, __instance, HexLand.PathFindingStartState.OnLand, ref list);
            HexLand last = Movement.Instance.m_HexListPartial.Last();
            sb.AppendLine($"path end: {last?.GetPosition()}"); // is giving correct last valid move hex for hex's to far
            MiniHexInfo poi = __instance.GetPOI();
            sb.AppendLine($"poi skill: {poi?.GetPOIProfile().m_SkillRequired}"); // fortitude
            sb.AppendLine($"poi display: {poi?.GetPOIDisplayValue()}"); // Cult Device
            if (TileHasQuestObjective(__instance, out QuestLogicBase quest))
            {
                sb.AppendLine($"quest desc: {StringReplace.RemoveStyling(quest.GetLocalizedOneLineDesc())}"); // Kill the <color=#FBB060>Chaos Leader</color> in <color=#FBB060>The Guardian Forest</color>
                QuestDefBase def = quest.GetQuestDef();
                if (def != null)
                {
                    sb.AppendLine($"def display: {def.m_DisplayName}"); // ""
                }
            }
            Plugin.Logger.LogMessage(sb.ToString());
        }


        static bool TileHasQuestObjective(HexLand hex, out QuestLogicBase quest)
        {
            MiniHexInfo poi = hex.GetPOI();
            quest = poi?.GetEncounterQuest();
            bool result = quest != null;
            if (!result)
            {
                if (poi?.GetFirstQuest() != null)
                {
                    quest = poi.GetFirstQuest();
                    result = true;
                }
            }
            return result;
        }


        [HarmonyPatch(typeof(uiPopupMenu), nameof(uiPopupMenu.Show))]
        [HarmonyPostfix]
        static void Popup1()
        {
            Plugin.Logger.LogWarning("popupMenu.Show");
        }

        [HarmonyPatch(typeof(uiPopupMenu), "OnClick")]
        [HarmonyPostfix]
        static void Popup2(uiPopupMenu.Action _a)
        {
            Plugin.Logger.LogWarning("popupMenu.OnClick " + _a);
        }

        [HarmonyPatch(typeof(uiItemMenu), "ShowBuyMenu")]
        [HarmonyPostfix]
        static void Popup3()
        {
            Plugin.Logger.LogWarning("uiItemMenu.ShowBuyMenu");
        }

        [HarmonyPatch(typeof(uiItemMenu), "ShowPlayerInventory")]
        [HarmonyPostfix]
        static void Popup5()
        {
            Plugin.Logger.LogWarning("uiItemMenu.ShowPlayerInventory");
        }

        [HarmonyPatch(typeof(uiItemMenu), "CheckScroll")] // end of Show
        [HarmonyPostfix]
        static void Popup4()
        {
            Plugin.Logger.LogWarning("uiItemMenu.CheckScroll");
        }

        // [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.OnEncounterSessionEnd))]
        // [HarmonyPostfix]
        // static void Test()
        // {
        //     Plugin.Logger.LogWarning("1 EncounterSession.OnEncounterSessionEnd");
        // }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.DungeonMiniEncounterCompleteMC))]
        [HarmonyPostfix]
        static void Test2()
        {
            Plugin.Logger.LogWarning("2 EncounterSessionMC.DungeonMiniEncounterCompleteMC");
        }

        // [HarmonyPatch(typeof(EncounterSessionMC), "FinalEncounterFinished")]
        // [HarmonyPostfix]
        // static void Test4()
        // {
        //     Plugin.Logger.LogWarning("4 EncounterSessionMC.FinalEncounterFinished");
        // }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.InitiateEncounterSessionRPC))] // entered dungeon
        [HarmonyPostfix]
        static void Test30()
        {
            Plugin.Logger.LogMessage("30 InitiateEncounterSessionRPC");
            ToggleOverworldActions.DisableOverworldActions();
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.InitiateNextEncounter))]
        [HarmonyPostfix]
        static void Test31()
        {
            Plugin.Logger.LogMessage("31 InitiateNextEncounter");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceRevealBattle))]
        [HarmonyPostfix]
        static void Test35()
        {
            Plugin.Logger.LogMessage("35 CommenceRevealBattle");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceMiniEncounterBattle))]
        [HarmonyPostfix]
        static void Test36()
        {
            Plugin.Logger.LogMessage("36 CommenceMiniEncounterBattle");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceStair))]
        [HarmonyPostfix]
        static void Test37()
        {
            Plugin.Logger.LogMessage("37 CommenceStair");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), "CommenceVoteEncounter")] // chest at end of dungeon
        [HarmonyPostfix]
        static void Test38()
        {
            Plugin.Logger.LogMessage("38 CommenceVoteEncounter");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceBattleRPC))] // context => enemy data
        [HarmonyPostfix]
        static void Test39()
        {
            Plugin.Logger.LogMessage("39 CommenceBattleRPC");
        }

        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.DisplayLootItem))]
        [HarmonyPostfix]
        static void Test47(string _item)
        {
            Plugin.Logger.LogMessage("47 DisplayLootItem: " + _item);
        }

        // [HarmonyPatch(typeof(MessagePresenter), nameof(MessagePresenter.DeliverMultiQuestMsgPart))]
        // [HarmonyPostfix]
        // static void MsgPresenterDeliverQuestPart() // may not be needed with uiPortraitMessageHud.InitializeMessage
        // {
        //     Plugin.Logger.LogMessage("MsgPresenterDeliverQuestPart");
        // }

        // [HarmonyPatch(typeof(MessagePresenter), nameof(MessagePresenter.DeliverStartQuestMsgPart))]
        // [HarmonyPostfix]
        // static void DeliverStartQuestMsgPart()
        // {
        //     Plugin.Logger.LogMessage("DeliverStartQuestMsgPart"); // sent after npc message init
        // }

        // [HarmonyPatch(typeof(MessagePresenter), nameof(MessagePresenter.DeliverQuestMessagePart))]
        // [HarmonyPostfix]
        // static void DeliverQuestMessagePart()
        // {
        //     Plugin.Logger.LogMessage("DeliverQuestMessagePart");
        // }

        // [HarmonyPatch(typeof(MessagePresenter), nameof(MessagePresenter.PresentMessage))]
        // [HarmonyPatch([typeof(int),typeof(Action),typeof(Action<ContinueFSM, int>),typeof(QuestLogicBase),typeof(GameEventManager.QuestMessageType),typeof(bool)])]
        // [HarmonyPostfix]
        // static void PresentMessage()
        // {
        //     Plugin.Logger.LogMessage("PresentMessage");
        // }


        // quest message
        [HarmonyPatch(typeof(MessagePresenter), "WaitPortraitToClose")]
        [HarmonyPatch([typeof(int),typeof(Action),typeof(Action<ContinueFSM, int>),typeof(QuestLogicBase),typeof(GameEventManager.QuestMessageType),typeof(bool)])]
        [HarmonyPostfix]
        static void PortraitClosed(QuestLogicBase _quest)
        {
            if (_quest == null) return;
            HexLand destination = _quest.GetHexLandDestination(); // null on quest complete
            if (destination == null) return;
            HexLandID id = destination.GetHexLandID();
            if (id == null) return;
            Plugin.Logger.LogMessage(destination + $" = {id.m_BigIndex} - {id.m_SmallIndex}");
            // CharacterOverworld character = GameLogic.Instance.GetCurrentCOW();
            // FTKPlayerID id2 = character.m_FTKPlayerID;
            // GameFlow.Instance.ToggleHexPingRPC(id2, id);
            // string name = character.m_CharacterStats.m_CharacterName;
            // FTK_playerGameStart.ID _class = character.m_CharacterStats.m_CharacterClass;
            // Plugin.Logger.LogMessage($"try ping {name} - {id.m_BigIndex} - {id.m_SmallIndex}");
        }

        // engage message
        [HarmonyPatch(typeof(MessagePresenter), "WaitPortraitToClose")]
        [HarmonyPatch([typeof(int),typeof(Action),typeof(Action<ContinueFSM, int>),typeof(MessageCoordinator.EngageMessageType),typeof(int),typeof(bool)])]
        [HarmonyPostfix]
        static void PortraitClosed2()
        {
            Plugin.Logger.LogMessage("engage message type");
            // public enum EngageMessageType
            // {
            // 	EnemySet,
            // 	DungeonMiniEncounterStart,
            // 	DungeonMiniEncounterEnd,
            // 	MessageID,
            // 	SessionDialogue
            // }
        }

        [HarmonyPatch(typeof(FTKUI), nameof(FTKUI.EnableEncounterMenu))]
        [HarmonyPostfix]
        static void EncounterMenu()
        {
            Plugin.Logger.LogWarning("TEST FTKUI.EnableEncounterMenu");
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Refresh))] // when shop item purchased
        [HarmonyPostfix]
        static void Location4()
        {
            // Plugin.Logger.LogWarning("UNKNOWN_loc_display_refresh");
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.OpenSubMenu))]
        [HarmonyPostfix]
        static void Location7()
        {
            Plugin.Logger.LogWarning("UNKNOWN_loc_display_OpenSubMenu");
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), "ShowSubMenu")] // maybe dropdown list?
        [HarmonyPostfix]
        static void Location5()
        {
            Plugin.Logger.LogWarning("UNKNOWN_loc_display_ShowSubMenu");
        }

        // // both responds are called?
        // [HarmonyPatch(typeof(CharacterDummy), nameof(CharacterDummy.RespondToDodge))]
        // [HarmonyPostfix]
        // static void Dodge(CharacterDummy __instance)
        // {
        //     Plugin.Logger.LogWarning("CharacterDummy.RespondToDodge");
        //     if (__instance.m_CharacterOverworld)
        //     {
        //         Plugin.Logger.LogWarning("player dodge " + AvoidResponse(__instance.m_DamageInfo.m_AttackResponse));
        //     }
        //     else
        //     {
        //         Plugin.Logger.LogWarning("enemy dodge " + AvoidResponse(__instance.m_DamageInfo.m_AttackResponse));
        //     }
        // }

        // [HarmonyPatch(typeof(CharacterDummy), nameof(CharacterDummy.RespondToHit))]
        // [HarmonyPostfix]
        // static void Hit(CharacterDummy __instance)
        // {
        //     Plugin.Logger.LogWarning("CharacterDummy.RespondToHit");
        //     if (__instance.m_CharacterOverworld)
        //     {
        //         Plugin.Logger.LogWarning("player hit " + AvoidResponse(__instance.m_DamageInfo.m_AttackResponse));
        //     }
        //     else
        //     {
        //         Plugin.Logger.LogWarning("enemy hit " + AvoidResponse(__instance.m_DamageInfo.m_AttackResponse));
        //     }
        // }


        static string AvoidResponse(CharacterDummy.AttackResponse response)
        {
            return response switch
            {
                CharacterDummy.AttackResponse.BlackHole => "BlackHole",
                CharacterDummy.AttackResponse.Block => "Block",
                CharacterDummy.AttackResponse.Dodge => "Dodge",
                CharacterDummy.AttackResponse.HarmlessAttack => "HarmlessAttack",
                CharacterDummy.AttackResponse.MagicBlock => "MagicBlock",
                CharacterDummy.AttackResponse.Petrify => "Petrify",
                CharacterDummy.AttackResponse.PetrifyBreak => "PetrifyBreak",
                CharacterDummy.AttackResponse.PetrifyBreakSanctum => "PetrifyBreakSanctum",
                CharacterDummy.AttackResponse.Protect => "Protect",
                CharacterDummy.AttackResponse.Reflect => "Reflect",
                CharacterDummy.AttackResponse.ResistDeath => "ResistDeath",
                CharacterDummy.AttackResponse.ResistDeathSanctum => "ResistDeathSanctum",
                CharacterDummy.AttackResponse.Shield => "Shield",
                CharacterDummy.AttackResponse.SteadFast => "Steadfast",
                _ => "",
            };
        }
        
        
    }
}