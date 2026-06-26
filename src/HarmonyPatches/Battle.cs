using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class Battle
    {
        static ActionWindow window;
        static uiBattleStanceButtons StanceButtonsInstance;
        static List<uiBattleStanceButtons.ProfValues> m_Proficiencies = [];

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Initialize))]
        [HarmonyPostfix]
        static void ButtonsInitialized(uiBattleStanceButtons __instance)
        {
            StanceButtonsInstance = __instance;
            Plugin.Logger.LogMessage("uiBattleStanceButtons Initialize");
        }

        [HarmonyPatch(typeof(FTKUI), nameof(FTKUI.EnableBattleStanceButtons))]
        [HarmonyPostfix]
        static void ButtonsEnabled()
        {
            window = CombatActions.RegisterActions(StanceButtonsInstance, m_Proficiencies);
        }

        [HarmonyPatch(typeof(uiBattleStanceButtons), "CreateWeaponProficiencyButtons")]
        [HarmonyPostfix]
        static void ProficiencyButtonsCreated(List<uiBattleStanceButtons.ProfValues> ___m_Proficiencies)
        {
            m_Proficiencies = [.. ___m_Proficiencies];
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.StartNextCombatRound2))] // before stance btns enable
        [HarmonyPostfix]
        static void NextCombatRound()
        {
            Plugin.Logger.LogMessage("StartNextCombatRound2");
            Object.Destroy(window);
        }


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
        //     Plugin.Logger.LogMessage($"DisplayBattleActionInfo: value: {dmg}; dmg title:{type}; desc:{desc} || {desc2}");
        // }

        // [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.BattleButtonsOff))] // called after attacks & game start
        // [HarmonyPostfix]
        // static void Test14()
        // {
        //     Plugin.Logger.LogMessage("14 BattleButtonsOff");
        //     UnityEngine.Object.Destroy(window);
        // }
    }

}