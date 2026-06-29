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
        static uiBattleStanceButtons StanceBtnInstance;
        static List<uiBattleStanceButtons.ProfValues> m_Proficiencies = [];

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Initialize))]
        [HarmonyPostfix]
        static void ButtonsInitialized(uiBattleStanceButtons __instance)
        {
            StanceBtnInstance = __instance;
            window = CombatActions.RegisterActions(StanceBtnInstance, m_Proficiencies);
        }

        [HarmonyPatch(typeof(uiBattleStanceButtons), "CreateWeaponProficiencyButtons")]
        [HarmonyPostfix]
        static void ProficiencyButtonsCreated(List<uiBattleStanceButtons.ProfValues> ___m_Proficiencies)
        {
            m_Proficiencies = [.. ___m_Proficiencies];
        }

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Attack))]
        [HarmonyPrefix]
        static void BtnsAttack(uiBattleStanceButtons __instance)
        {
            Plugin.Logger.LogWarning("4 stanceBtnsAttack");
            if (__instance.CombatCow.m_CurrentDummy is EnemyDummy)//TODO only called by players
            {
                Plugin.Logger.LogWarning("enemy attacks " + (__instance.CombatCow.m_CurrentDummy as EnemyDummy).m_EnemyCombat.GetEnemyDisplay());
            }
            else
            {
                Plugin.Logger.LogWarning("attacker player " + __instance.CombatCow.m_CurrentDummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName);
            }
        }

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.BattleButtonsOff))]
        [HarmonyPrefix]
        static void BtnsOff()
        {
            Object.Destroy(window);
        }

        // [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.StartNextCombatRound2))] // before stance btns enable
        // [HarmonyPostfix]
        // static void NextCombatRound()
        // {
        //     Plugin.Logger.LogMessage("StartNextCombatRound2");
        //     Object.Destroy(window);
        // }


#region new

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyFlee))] // context
        [HarmonyPostfix]
        static void Test44()
        {
            Plugin.Logger.LogMessage("44 CombatEnemyFlee");
            Context.Send("the enemy has fled");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyDie))] // context
        [HarmonyPostfix]
        static void Test45(FTKPlayerID _victim, FTKPlayerID _attacker) //NullReferenceException: Object reference not set to an instance of an object
        {
            Plugin.Logger.LogWarning(_victim);
            FTKPlayerID ph = _victim;
            var t = ph.GetCow();
            Plugin.Logger.LogWarning(t);
            var u = t.m_CurrentDummy;
            Plugin.Logger.LogWarning(u);
            var w = (u as EnemyDummy).m_EnemyCombat;
            Plugin.Logger.LogWarning(w);
            var x = w.GetEnemyDisplay();
            Plugin.Logger.LogWarning(x);
            string victim = (ph.GetCow()?.m_CurrentDummy as EnemyDummy)?.m_EnemyCombat?.GetEnemyDisplay();
            Context.Send($"[enemy] {victim} has died");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerDie))] // context
        [HarmonyPostfix]
        static void Test46(FTKPlayerID _victim, FTKPlayerID _attacker)
        {
            FTKPlayerID ph = _victim;
            string victim = ph.GetCow().m_CurrentDummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName;
            Context.Send($"{victim} has died");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerVictory))]
        [HarmonyPostfix]
        static void CombatPlayerVictory()
        {
            Plugin.Logger.LogWarning("battle combat victory");
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