using System;
using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    /// <summary>
    /// add patches to track when they occur
    /// </summary>
    [HarmonyPatch]
    public class Tests
    {

        [HarmonyPatch(typeof(uiPopupMenu), nameof(uiPopupMenu.Show))] // click item in inventory 
        [HarmonyPostfix]
        static void Popup1()
        {
            Plugin.Logger.LogWarning("popupMenu.Show");
        }

        [HarmonyPatch(typeof(uiPopupMenu), "OnClick")] // btns from popup menu of item
        [HarmonyPostfix]
        static void Popup2(uiPopupMenu.Action _a)
        {
            Plugin.Logger.LogWarning("popupMenu.OnClick " + _a); // close, equip
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

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.DungeonMiniEncounterCompleteMC))]
        [HarmonyPostfix]
        static void Test2()
        {
            Plugin.Logger.LogWarning("2 EncounterSessionMC.DungeonMiniEncounterCompleteMC");
            // after quest dungeon
            
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

        // [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.DungeonEncounter))]
        // [HarmonyPrefix]
        // static void Test1(CharacterOverworld __instance)
        // {
        //     Plugin.Logger.LogMessage("dungeon encounter: " + CharacterData.GetCharacterName(__instance));
        // }

        [HarmonyPatch(typeof(uiEnterDungeonMenu), nameof(uiEnterDungeonMenu.OnEnter))]
        [HarmonyPrefix]
        static void Test4()
        {
            Plugin.Logger.LogMessage("enter dungeon menu OnEnter");
        }

        [HarmonyPatch(typeof(uiEnterDungeonMenu), nameof(uiEnterDungeonMenu.OnLeave))]
        [HarmonyPostfix]
        static void Test5()
        {
            Plugin.Logger.LogMessage("enter dungeon menu OnLeave");
        }


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
            // CharacterOverworld character = CharacterData.GetActiveCow();
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