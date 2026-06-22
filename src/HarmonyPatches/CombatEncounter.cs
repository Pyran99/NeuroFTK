using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CombatEncounter
    {

        static ActionWindow window;

#region Main

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EnableMenu))]
        [HarmonyPostfix]
        static void EncounterMenuEnabled()
        {
            Plugin.Logger.LogMessage("NYI uiEncounterMenu.EnableMenu => send neuro action of choices");
            ToggleOverworldActions.DisableOverworldActions();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void Test5()
        {
            Plugin.Logger.LogMessage("8 disable menu");
            // ToggleOverworldActions.EnableOverworldActions(); //TODO enable from elsewhere, this is called for leave & enter encounter
        }

        static uiBattleStanceButtons instance;

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Initialize))]
        [HarmonyPostfix]
        static void Test8(uiBattleStanceButtons __instance)
        {
            instance = __instance;
            Plugin.Logger.LogMessage("9 uiBattleStanceButtons Initialize => send neuro action?");
        }

        [HarmonyPatch(typeof(FTKUI), nameof(FTKUI.EnableBattleStanceButtons))]
        [HarmonyPostfix]
        static void Test48()
        {
            Plugin.Logger.LogMessage("10 EnableBattleStanceButtons => NeuroAction");
            window = CombatActions.RegisterActions(instance, m_Proficiencies);

            // ActionWindow window = ActionWindow.Create(GameLogic.Instance.GetCurrentCombatCOW().gameObject);
            // Weapon wpn = GameLogic.Instance.GetCurrentCombatCOW().m_CurrentDummy.m_EventListener.m_Weapon;
            // int capacity = wpn.m_AmmoCapacity;
        }

        static List<uiBattleStanceButtons.ProfValues> m_Proficiencies = [];

        [HarmonyPatch(typeof(uiBattleStanceButtons), "CreateWeaponProficiencyButtons")]
        [HarmonyPostfix]
        static void Test10(List<uiBattleStanceButtons.ProfValues> ___m_Proficiencies, Weapon _weapon)
        {
            Plugin.Logger.LogMessage("12 CreateWeaponProficiencyButtons");
            m_Proficiencies = [.. ___m_Proficiencies];
        }

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.AttackProficiency))]
        [HarmonyPostfix]
        static void Test13(uiBattleButton _button)
        {
            Plugin.Logger.LogMessage("20 AttackProficiency"); // after attack attempt
        }


#endregion


        #region new

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.InitiateEncounterSessionRPC))]
        [HarmonyPostfix]
        static void Test30()
        {
            Plugin.Logger.LogMessage("30 InitiateEncounterSessionRPC");
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

        [HarmonyPatch(typeof(EncounterSessionMC), "CommenceVoteEncounter")]
        [HarmonyPostfix]
        static void Test38()
        {
            Plugin.Logger.LogMessage("38 CommenceVoteEncounter");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceBattleRPC))]
        [HarmonyPostfix]
        static void Test39()
        {
            Plugin.Logger.LogMessage("39 CommenceBattleRPC");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerVictory))]
        [HarmonyPostfix]
        static void Test46()
        {
            Plugin.Logger.LogMessage("46 CombatPlayerVictory");
        }

        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.DisplayLootItem))]
        [HarmonyPostfix]
        static void Test47(string _item)
        {
            Plugin.Logger.LogMessage("47 DisplayLootItem: " + _item);
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyFlee))] // context
        [HarmonyPostfix]
        static void Test44()
        {
            Plugin.Logger.LogMessage("44 CombatEnemyFlee");
            Context.Send("the enemy has fled");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyDie))] // context
        [HarmonyPostfix]
        static void Test45()
        {
            Plugin.Logger.LogMessage("45 CombatEnemyDie");
            Context.Send("the enemy has died");
        }

        #endregion



        #region may have uses

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.BattleButtonsOff))] // called after attacks
        [HarmonyPostfix]
        static void Test14()
        {
            Plugin.Logger.LogMessage("14 BattleButtonsOff");
            UnityEngine.Object.Destroy(window);
        }

        // [HarmonyPatch(typeof(EncounterSessionMC), "CommenceBattle")]
        // [HarmonyPostfix]
        // static void Test33()
        // {
        //     Plugin.Logger.LogMessage("33 CommenceBattle");
        // }

        // [HarmonyPatch(typeof(EncounterSessionMC), "CommenceBattle2")]
        // [HarmonyPostfix]
        // static void Test34()
        // {
        //     Plugin.Logger.LogMessage("34 CommenceBattle2");
        // }

        // // shows/hide icons
        // [HarmonyPatch(typeof(uiBattleButtonInfoPanel), nameof(uiBattleButtonInfoPanel.SetDisplay))]
        // [HarmonyPostfix]
        // static void Test17(uiBattleButtonInfoPanel __instance, CharacterOverworld _cow, FTK_weaponStats2.SkillType _skill, FTK_weaponStats2.DamageType _dmgType, FTK_proficiencyTable.ID _prof, uiBattleButton _battleButton)
        // {
        //     // some things are set after from DisplayBattleActionInfo
        // }

        // [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.DisplayBattleActionInfo))] // spam while mouse down enemy
        // [HarmonyPostfix]
        // static void Test11(uiBattleStanceButtons __instance, bool _on)
        // {
        //     if (!_on) return;
        //     FTK_weaponStats2 entry = FTK_weaponStats2DB.GetDB().GetEntry(__instance.CombatCow.m_WeaponID);
        //     FTK_weaponStats2.DamageType dmgType = entry._dmgtype;
        //     uiBattleButtonInfoPanel info = __instance.m_InfoPanel;
        //     string type = dmgType == FTK_weaponStats2.DamageType.physical ? FTKHub.Localized<TextMenu>("STR_battleButtonsPhysDmg") : FTKHub.Localized<TextMenu>("STR_battleButtonsMagDmg");
        //     string dmg = info.m_DamageValue.text;
        //     string desc = info.m_Description[0]?.text ?? "null";
        //     string desc2 = info.m_Description[1]?.text ?? "null";
        //     //TODO per slot ACC
        //     Plugin.Logger.LogMessage($"DisplayBattleActionInfo: value: {dmg}; dmg title:{type}; desc:{desc} || {desc2}");
        // }

        // [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.StartNextCombatRound))]
        // [HarmonyPostfix]
        // static void Test41()
        // {
        //     Plugin.Logger.LogMessage("41 StartNextCombatRound");
        // }

        // [HarmonyPatch(typeof(uiBattleButton), nameof(uiBattleButton.OnSelect))] // mouse hovers
        // [HarmonyPostfix]
        // static void Test15()
        // {
        //     Plugin.Logger.LogMessage("17 OnSelect");
        // }

        // entered combat hex
        [HarmonyPatch(typeof(uiEncounterMenu), "SetMenuPanelMode")] // may have uses
        [HarmonyPostfix]
        static void Test3(uiEncounterMenu __instance)
        {
            MiniHexInfo.MiniHexType type = __instance.m_ThisMiniHex.m_MiniHexType;
            Plugin.Logger.LogMessage("1 set menu panel mode");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))] // may have uses
        [HarmonyPostfix]
        static void Test2()
        {
            Plugin.Logger.LogMessage("2 menu refresh");
        }

        #endregion



        #region other/no use

        // [HarmonyPatch(typeof(uiEachEnemyHud), nameof(uiEachEnemyHud.Initialize))]
        // [HarmonyPostfix]
        // static void Test21()
        // {
        //     Plugin.Logger.LogMessage("6 Initialize");
        // }

        // [HarmonyPatch(typeof(uiEnemyHUD), nameof(uiEnemyHUD.InitializeEnemyHud))]
        // [HarmonyPostfix]
        // static void Test18()
        // {
        //     Plugin.Logger.LogMessage("7 InitializeEnemyHud");
        // }

        // [HarmonyPatch(typeof(uiEachEnemyHud), "RefreshStatusHudIcons")]
        // [HarmonyPostfix]
        // static void Test23()
        // {
        //     Plugin.Logger.LogMessage("5 RefreshStatusHudIcons");
        // }

        // [HarmonyPatch(typeof(uiEnemyHUD), nameof(uiEnemyHUD.SetEnemyHealth))]
        // [HarmonyPostfix]
        // static void Test19()
        // {
        //     Plugin.Logger.LogMessage("9 SetEnemyHealth");
        // }

        // [HarmonyPatch(typeof(uiEachEnemyHud), nameof(uiEachEnemyHud.Show))]
        // [HarmonyPostfix]
        // static void Test22(EnemyDummy _ed)
        // {
        //     Plugin.Logger.LogMessage("10 EachEnemyHudShow");
        // }

        // [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.SelectEnemyDummy))]
        // [HarmonyPostfix]
        // static void Test14(FTKPlayerID _victim, FTK_itembase.ID _item = FTK_itembase.ID.None)
        // {
        //     Plugin.Logger.LogMessage("11 SelectEnemyDummy");
        // }

        // [HarmonyPatch(typeof(uiBattleButton), nameof(uiBattleButton.OnDeselect))]
        // [HarmonyPostfix]
        // static void Test16()
        // {
        //     Plugin.Logger.LogMessage("18 OnDeselect");
        // }

        // [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.GetCurrentEncounter))] // multiple calls before encounter menu disabled
        // [HarmonyPostfix]
        // static void Test40()
        // {
        //     Plugin.Logger.LogMessage("40 GetCurrentEncounter");
        // }

        // [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.InitiateCurrentEncounter))] // before encounter menu disabled
        // [HarmonyPostfix]
        // static void Test32()
        // {
        //     Plugin.Logger.LogMessage("32 InitiateCurrentEncounter");
        // }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.StartNextCombatRound2))] // before stance btns enable
        [HarmonyPostfix]
        static void Test42()
        {
            Plugin.Logger.LogMessage("42 StartNextCombatRound2");
        }



        #endregion



        #region not called

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Attack))]
        [HarmonyPostfix]
        static void Test12()
        {
            Plugin.Logger.LogMessage("19 Attack");
        }

        // [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.ResumeAfterRadiusFlasher))]
        // [HarmonyPostfix]
        // static void Test1()
        // {
        //     Plugin.Logger.LogMessage("1 resume after radius flasher");
        // }

        // [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.OpenOverworldTreasureChest))]
        // [HarmonyPostfix]
        // static void Test7()
        // {
        //     Plugin.Logger.LogMessage("7 OpenOverworldTreasureChest");
        // }

        // [HarmonyPatch(typeof(uiEnemyHUD), nameof(uiEnemyHUD.TurnOffEachEnemyHuds))]
        // [HarmonyPostfix]
        // static void Test20()
        // {
        //     Plugin.Logger.LogMessage("20 TurnOffEachEnemyHuds");
        // }

        // [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.LeaveOrEndTurn))]
        // [HarmonyPostfix]
        // static void Test6()
        // {
        //     Plugin.Logger.LogMessage("4 leaveOrEndTurn");
        // }

        #endregion

        

    }
}