using System;
using System.Linq;
using Google2u;
using GridEditor;
using HarmonyLib;
using Pyran.NeuroFTK.NeuroIntegration;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    /// <summary>
    /// add patches to track when they occur
    /// </summary>
    [HarmonyPatch]
    public class Tests
    {

        [HarmonyPatch(typeof(HexLand), nameof(HexLand.Ping))]
        [HarmonyPostfix]
        static void Ping(HexLand __instance)
        {
            Plugin.Logger.LogMessage($"{__instance.GetHexLandID().m_BigIndex} - {__instance.GetHexLandID().m_SmallIndex} ping");
            Plugin.Logger.LogMessage($"{__instance.GetPosition()} ping");
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
            Plugin.Logger.LogMessage("quest message type");
            Plugin.Logger.LogMessage(_quest);
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
// [Message:Neuro For the King] quest message type
// [Message:Neuro For the King] MultiQuestLogic
// [Message:Neuro For the King] 1
// [Message:Neuro For the King] PresentMessage
// [Message:Neuro For the King] show quest message
// [Message:Neuro For the King] -1 - -1
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
        
        
    }
}