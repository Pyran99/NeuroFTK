using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using UnityEngine;
using Pyran.NeuroFTK.GameConfigs;

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

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.BattleButtonsOff))]
        [HarmonyPrefix]
        static void BtnsOff()
        {
            Object.Destroy(window);
            m_Proficiencies = [];
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyFlee))]
        [HarmonyPostfix]
        static void EnemyFled(FTKPlayerID _enemyID)
        {
            CharacterDummy dummy = EncounterSession.Instance.GetDummyByFID(_enemyID);
            if (dummy == null)
            {
                return;
            }
            string enemy = (dummy as EnemyDummy).m_EnemyCombat.GetEnemyDisplay();
            Context.Send($"[enemy] {enemy} has fled the battle");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyDie))]
        [HarmonyPostfix]
        static void EnemyDied(FTKPlayerID _victim, FTKPlayerID _attacker)
        {
            CharacterDummy dummy = EncounterSession.Instance.GetDummyByFID(_victim);
            if (dummy == null)
            {
                return;
            }
            Context.Send($"[enemy] {GetEnemyName(dummy as EnemyDummy)} has died");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyBlackHoled))]
        [HarmonyPostfix]
        static void CombatEnemyBlackHoled(FTKPlayerID _enemyID)
        {
            CharacterDummy dummy = EncounterSession.Instance.GetDummyByFID(_enemyID);
            if (dummy == null)
            {
                Plugin.Logger.LogError("null dummy");
                return;
            }
            Context.Send($"[enemy] {GetEnemyName(dummy as EnemyDummy)} was consumed by a black hole");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerDie))]
        [HarmonyPostfix]
        static void PlayerDied(FTKPlayerID _victim, FTKPlayerID _attacker)
        {
            FTKPlayerID ph = _victim;
            string victim = ph.GetCow().m_CurrentDummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName;
            Context.Send($"{victim} has died");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerVictory))]
        [HarmonyPostfix]
        static void CombatPlayerVictory()
        {
            if (ToggleOverworldActions.mode == uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogMessage("combat victory overworld skip");
                return;
            }
            Context.Send("you have won the battle!");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerFlee))]
        [HarmonyPostfix]
        static void CombatPlayerFled(FTKPlayerID _fid)
        {
            FTKPlayerID ph = _fid;
            string player = ph.GetCow().m_CurrentDummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName;
            Context.Send($"[player] {player} has fled the battle");
        }

        static string GetEnemyName(EnemyDummy _dummy)
        {
            if (!uiEnemyHUD.Instance.m_EnemyHudDictionary.ContainsKey(_dummy))
            {
                Plugin.Logger.LogError($"invalid dummy ui {_dummy?.m_EnemyCombat?.GetEnemyDisplay()}");
                return "";
            }
            uiEachEnemyHud hud = uiEnemyHUD.Instance.m_EnemyHudDictionary[_dummy];
            string name = hud.m_EnemyNameDisplay.text;
            return StringReplace.ReplaceNewLineSpace(name);
        }

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