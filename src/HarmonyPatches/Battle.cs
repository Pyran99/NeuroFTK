using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using UnityEngine;
using GridEditor;
using Google2u;
using System.Linq;
using Pyran.NeuroFTK.GameConfigs;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class Battle
    {
        static ActionWindow window;
        static uiBattleStanceButtons StanceBtnInstance;
        static List<uiBattleStanceButtons.ProfValues> m_Proficiencies = [];
        static Dictionary<string, uiBattleButton> offense = [];
        static Dictionary<string, uiBattleButton> defense = [];

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Initialize))]
        [HarmonyPostfix]
        static void ButtonsInitialized(uiBattleStanceButtons __instance)
        {
            StanceBtnInstance = __instance;
            CreateActionWindow(StanceBtnInstance, m_Proficiencies);
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
        [HarmonyPrefix]
        static void CombatPlayerVictory()
        {
            if (ToggleOverworldActions.mode == uiGameTrackerHUD.GameTrackerMode.Overworld) // changed before post-call
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

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GainSpecificHealth))]
        [HarmonyPostfix]
        static void HealthChanged(CharacterStats __instance, int _hpGain)
        {
            Plugin.Logger.LogWarning($"{__instance.m_CharacterName} health gain {_hpGain}");
            CombatEvents.OnDamageTaken(__instance.m_CharacterOverworld, _hpGain, __instance.m_HealthCurrent, __instance.MaxHealth);
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetSpecificHealthRPC))] // secondarydmg calls rpc directly | players only
        [HarmonyPrefix]
        static void HealthChanged2(CharacterStats __instance, int _newHp)
        {
            Plugin.Logger.LogWarning($"{__instance.m_CharacterName} health set rpc {_newHp}");
            // int _dmg = Mathf.Clamp(newHp, 0, __instance.MaxHealth) - __instance.m_HealthCurrent;
            // CombatEvents.OnDamageTaken(__instance.m_CharacterOverworld, _dmg, __instance.m_HealthCurrent, __instance.MaxHealth);
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TakeSecondaryDamageCombat))]
        [HarmonyPrefix]
        static void HealthChanged3(CharacterStats __instance, int _dmg)
        {
            Plugin.Logger.LogWarning($"{__instance.m_CharacterName} second dmg {_dmg}");
            // int _dmg = Mathf.Clamp(newHp, 0, __instance.MaxHealth) - __instance.m_HealthCurrent;
            // CombatEvents.OnDamageTaken(__instance.m_CharacterOverworld, _dmg, __instance.m_HealthCurrent, __instance.MaxHealth);
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TakeSecondaryDamageNonCombatRPC))]
        [HarmonyPrefix]
        static void HealthChanged4(CharacterStats __instance, int _dmg)
        {
            Plugin.Logger.LogWarning($"{__instance.m_CharacterName} second dmg rpc {_dmg}");
            // int _dmg = Mathf.Clamp(newHp, 0, __instance.MaxHealth) - __instance.m_HealthCurrent;
            // CombatEvents.OnDamageTaken(__instance.m_CharacterOverworld, _dmg, __instance.m_HealthCurrent, __instance.MaxHealth);
        }

        [HarmonyPatch(typeof(EnemyDummy), nameof(EnemyDummy.GainSpecificHealth))]
        [HarmonyPostfix]
        static void EnemyDmgTaken(EnemyDummy __instance, int _gain)
        {
		// this.m_CurrentHealth = Mathf.Clamp(this.m_CurrentHealth + _gain, 0, this.m_EnemyCombat.GetHealthTotal());
		// EncounterSession.Instance.UpdateEnemyHealthRPC(this.FID, this.m_CurrentHealth);
        }

        [HarmonyPatch(typeof(EnemyDummy), nameof(EnemyDummy.TakeSecondaryDamage))] // does not call Gain, calls EncounterSession.Instance.UpdateEnemyHealthRPC(this.FID, this.m_CurrentHealth);
        [HarmonyPostfix]
        static void EnemyDmgTaken2(EnemyDummy __instance, int _dmg, FTKPlayerID _attackerID)
        {
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

#region action window

        static List<INeuroAction> actions = [];

        static void CreateActionWindow(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> _proficiencies)
        {
            GetOffenseAttackDetails(_instance, _proficiencies);
            GetDefenseAttackDetails(_instance, _proficiencies);
            string ctx = GetContext(_instance, _proficiencies);
            window = CombatActions.CreateAction(_instance, ctx, actions);
            offense.Clear();
            defense.Clear();
        }

        static string GetContext(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> _proficiencies)
        {
            actions.Clear();
            string ctx = "";
            if (offense.Count > 0)
            {
                foreach (string key in offense.Keys)
                {
                    var data = GetActionDetails(offense[key], _proficiencies);
                    ctx += AddAttackContext(data);
                }
                actions.Add(new CombatAttackAction(offense));
            }
            if (defense.Count > 0)
            {
                foreach (string key in defense.Keys)
                {
                    var data = GetActionDetails(defense[key], _proficiencies);
                    ctx += AddAttackContext(data);
                }
                actions.Add(new CombatFriendlyAction(defense));
            }
            if (CanUseBtn(_instance.m_FleeButton) && !GlobalConfig.debug_mode)
            {
                ctx += HandleBtnContext(_instance.m_FleeButton, _proficiencies);
                actions.Add(new CombatFleeAction(_instance.m_FleeButton));
            } 
            if (CanUseBtn(_instance.m_ReviveButton))
            {
                ctx += HandleBtnContext(_instance.m_ReviveButton, _proficiencies, false);
                actions.Add(new CombatReviveAction(_instance.m_ReviveButton));
            }
            if (CanUseBtn(_instance.m_ShieldTauntButton))
            {
                ctx += HandleBtnContext(_instance.m_ShieldTauntButton, _proficiencies);
                actions.Add(new CombatTauntAction(_instance.m_ShieldTauntButton));
            }
            if (CanUseBtn(_instance.m_EquipWeaponButton) && !GlobalConfig.debug_mode)
            {
                ctx += HandleBtnContext(_instance.m_EquipWeaponButton, _proficiencies, false);
                actions.Add(new CombatChangeWeaponAction(_instance.m_EquipWeaponButton));
            }
            if (CanUseBtn(_instance.m_PartyHealButton))
            {
                ctx += HandleBtnContext(_instance.m_PartyHealButton, _proficiencies, false);
                actions.Add(new CombatPartyHealAction(_instance.m_PartyHealButton));
            }
            return ctx;
        }

        static bool CanUseBtn(uiBattleButton btn)
        {
            return btn != null && btn.isActiveAndEnabled && btn.m_CanUse;
        }

        static string HandleBtnContext(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> m_Proficiencies, bool hasRolls = true)
        {
            var data = GetActionDetails(btn, m_Proficiencies);
            return AddContext(data, hasRolls);
        }

        public static void GetOffenseAttackDetails(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> _Proficiencies)
        {
            offense.Clear();
            Dictionary<string, uiBattleButton> btns = [];
            bool useDefault = _instance.m_AttackButton != null && _instance.m_AttackButton.m_CanUse && _instance.m_AttackButton.isActiveAndEnabled; // this may act as normal attack with/out weapon
            bool canReload = _instance.m_ReloadButton != null && _instance.m_ReloadButton.m_CanUse && _instance.m_ReloadButton.isActiveAndEnabled;
            if (useDefault)
            {
                FTK_weaponStats2 entry1 = FTK_weaponStats2DB.GetDB().GetEntry(GameLogic.Instance.GetCurrentCombatCOW().m_WeaponID);
                btns.Add(entry1.GetAttackDisplay(), _instance.m_AttackButton);
            }
            if (canReload)
            {
                btns.Add(FTKHub.Localized<TextMenu>("STR_battleButtonsReloadWeapon"), _instance.m_ReloadButton);
            }
            foreach (uiBattleStanceButtons.ProfValues prof in _Proficiencies)
            {
                switch (prof.m_Button.m_ButtonType)
                {
                    case uiBattleButton.BattleButtonType.flee:
                    case uiBattleButton.BattleButtonType.shieldtaunt:
                    case uiBattleButton.BattleButtonType.equipweapon:
                    case uiBattleButton.BattleButtonType.revive:
                    case uiBattleButton.BattleButtonType.partyheal:
                        continue;
                }
                if (prof.m_Prof == FTK_proficiencyTable.ID.None) continue;
                FTK_proficiencyTable entry = FTK_proficiencyTableDB.GetDB().GetEntry(prof.m_Prof);
                if (entry.m_TargetFriendly) continue;
                string name = entry.GetLocalizedDisplayTitle();
                if (prof.m_Button == null || !prof.m_Button.m_CanUse || !prof.m_Button.isActiveAndEnabled) continue;
                if (btns.ContainsKey(name))
                {
                    Plugin.Logger.LogWarning($"existing key {name}");
                    continue;
                }
                btns.Add(name, prof.m_Button);
            }
            offense = new Dictionary<string, uiBattleButton>(btns);
        }

        public static void GetDefenseAttackDetails(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> _Proficiencies)
        {
            // GetTargetInfo(FTK_proficiencyTable.ID pid, out CharacterDummy.TargetType _targetType, out bool _targetFriendly)
            defense.Clear();
            Dictionary<string, uiBattleButton> btns = [];
            foreach (uiBattleStanceButtons.ProfValues prof in _Proficiencies)
            {
                if (prof.m_Prof == FTK_proficiencyTable.ID.None) continue;
                FTK_proficiencyTable table = FTK_proficiencyTableDB.Get(prof.m_Prof);
                if (!table.m_TargetFriendly) continue;
                if (prof.m_Button == null || !prof.m_Button.m_CanUse || !prof.m_Button.isActiveAndEnabled) continue;
                string name = table.GetLocalizedDisplayTitle();
                if (btns.ContainsKey(name))
                {
                    Plugin.Logger.LogWarning($"existing key {name}");
                    continue;
                }
                btns.Add(name, prof.m_Button);
            }
            defense = new Dictionary<string, uiBattleButton>(btns);
        }

        /// <summary>
        /// dictionary style: "name": {"type": "target self", "description": "perfect(56%) = leave combat", "per_roll_chance": "50", "damage": "10"}
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> GetActionDetails(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> _Proficiencies)
        {
            // from => public void DisplayBattleActionInfo(uiBattleButton _button, bool _on)
            Dictionary<string, Dictionary<string, string>> data = [];
            CharacterOverworld current = GameLogic.Instance.GetCurrentCombatCOW();
            global::CharacterStats stats = current.m_CharacterStats;
			FTK_weaponStats2 entry = FTK_weaponStats2DB.GetDB().GetEntry(current.m_WeaponID);
			FTK_proficiencyTable.ID id = FTK_proficiencyTable.ID.None;
            FTK_weaponStats2.SkillType skillType;
            int maxRollSlots = GameFlow.Instance.m_DefaultSlots;
            string text = "";
            string[] array = [null, FTKHub.Localized<TextMenu>("STR_battleButtonsStandardAttack")];

            switch (btn.m_ButtonType)
            {
                case uiBattleButton.BattleButtonType.flee:
                    skillType = FTK_weaponStats2.SkillType.quickness;
                    if (stats.m_CharacterSkills.m_Flee)
                    {
                        maxRollSlots = 1;
                        text = FTKHub.Localized<TextMenu>("STR_battleButtonsEliteFlee");
                    }
                    else
                    {
                        text = FTKHub.Localized<TextMenu>("STR_battleButtonsFlee");
                    }
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsTargetSelf");
                    if (EncounterSession.Instance.CanPlayerFlee(current))
                    {
                        float num4 = stats.CalculateFullSkillChance(skillType, maxRollSlots, 0f);
                        string text2 = FTKHub.Localized<TextMenu>("STR_battleButtonsLeaveCombat");
                        array[1] = FTKUI.GetPerfectDescriptionFormatted(num4, text2);
                    }
                    else
                    {
                        array[1] = FTKHub.Localized<TextMenu>("STR_battleButtonsNoFlee");
                    }
                    break;
                case uiBattleButton.BattleButtonType.shieldtaunt:
                    text = FTKHub.Localized<TextMisc>("STR_profTaunt");
                    skillType = FTK_weaponStats2.SkillType.vitality;
                    maxRollSlots = GameFlow.Instance.m_DefaultSlots;
                    float fullSkillChance = current.m_CharacterStats.CalculateFullSkillChance(skillType, maxRollSlots, 0f);
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsTargetSelf");
                    array[1] = FTKUI.GetPerfectDescriptionFormatted(fullSkillChance, FTKHub.Localized<TextMenu>("STR_battleButtonsDrawAttention"));
                    break;
                case uiBattleButton.BattleButtonType.attack:
                    maxRollSlots = entry._slots;
                    text = entry.GetAttackDisplay();
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsSingleTarget");
                    break;
                case uiBattleButton.BattleButtonType.proficiency:
                    foreach (uiBattleStanceButtons.ProfValues profValues in _Proficiencies)
                    {
                        if (profValues.m_Button == btn)
                        {
                            id = profValues.m_Prof;
                            break;
                        }
                    }
                    if (id == FTK_proficiencyTable.ID.None) Plugin.Logger.LogError("proficiency error");
					FTK_proficiencyTable entry3 = FTK_proficiencyTableDB.GetDB().GetEntry(id);
					if (entry3.m_SlotOverride > 0)
					{
						maxRollSlots = entry3.m_SlotOverride;
					}
					else
					{
						maxRollSlots = entry._slots;
					}
					text = entry3.GetLocalizedDisplayTitle();
					array = entry3.GetBattleButtonInfo(current);
                    break;
                case uiBattleButton.BattleButtonType.equipweapon:
                    text = FTKHub.Localized<TextMenu>("STR_battleButtonsEquipWeapon");
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsTargetSelf");
                    array[1] = FTKHub.Localized<TextMenu>("STR_equipWeaponDescription");
                    break;
                case uiBattleButton.BattleButtonType.partyheal:
                    text = FTKHub.Localized<TextMenu>("STR_battleButtonsPartyHeal");
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsTargetParty");
                    if (current.HasInventoryItem(FTK_itembase.ID.herbGodsbeard1))
                    {
                        array[1] = FTKHub.Localized<TextMenu>("STR_battleButtonsPartyHealGodsbeard");
                    }
                    else
                    {
                        array[1] = FTKHub.Localized<TextMenu>("STR_battleButtonsPartyHealNoGodsbeard");
                    }
                    break;
                case uiBattleButton.BattleButtonType.reload:
                    text = FTKHub.Localized<TextMenu>("STR_battleButtonsReloadWeapon");
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsTargetSelf");
                    if (current.GetCurrentDummy().m_CurrentAmmo == 0)
                    {
                        array[1] = FTKHub.Localized<TextMenu>("STR_battleButtonsYouMustReloadWeapon");
                    }
                    else
                    {
                        array[1] = FTKHub.Localized<TextMenu>("STR_battleButtonsNoReload");
                    }
                    break;
                case uiBattleButton.BattleButtonType.revive:
                    text = FTKHub.Localized<TextMenu>("STR_RevivePlayer");
                    array[0] = FTKHub.Localized<TextMenu>("STR_battleButtonsTargetFriendly");
                    array[1] = FTKHub.Localized<TextMenu>("STR_battleButtonsReviveInfo");
                    break;
                default:
                    Plugin.Logger.LogError("invalid button type or standard attack");
                    break;
            }
            // maxRolls = maxRollSlots;
            data.Add(text, []);
            data[text]["type"] = array[0];
            data[text]["description"] = array[1];
            data[text]["per_roll_chance"] = "100%";
            float accuracy = GetAccuracy(btn, _Proficiencies);
            if (accuracy > -1f) data[text]["per_roll_chance"] = FTKUtil.RoundToInt(accuracy * 100f).ToString() + "%";
            data[text]["damage"] = GetAttackDamage(btn, _Proficiencies).ToString();
            return data;
        }

        public static float GetAccuracy(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> _Proficiencies)
        {
            CharacterOverworld current = GameLogic.Instance.GetCurrentCombatCOW();
            global::CharacterStats stats = current.m_CharacterStats;
			FTK_weaponStats2 entry = FTK_weaponStats2DB.GetDB().GetEntry(current.m_WeaponID);
			FTK_weaponStats2.SkillType skillType;
            FTK_proficiencyTable.ID id = FTK_proficiencyTable.ID.None;
            float acc = -1;

            switch (btn.m_ButtonType)
            {
                case uiBattleButton.BattleButtonType.flee:
                    skillType = FTK_weaponStats2.SkillType.quickness;
                    acc = stats.GetSkillValue(skillType, true, 0f);
                    break;
                case uiBattleButton.BattleButtonType.shieldtaunt:
                    skillType = FTK_weaponStats2.SkillType.vitality;
                    FTK_proficiencyTable entry2 = FTK_proficiencyTableDB.GetDB().GetEntry(FTK_proficiencyTable.ID.taunt);
                    acc = stats.GetSkillValue(skillType, true, entry2.m_PerSlotSkillRoll);
                    break;
                case uiBattleButton.BattleButtonType.attack:
                    acc = stats.GetSkillValue(entry._skilltest, true, 0f);
                    break;
                case uiBattleButton.BattleButtonType.proficiency:
                    foreach (uiBattleStanceButtons.ProfValues profValues in _Proficiencies)
                    {
                        if (profValues.m_Button == btn)
                        {
                            id = profValues.m_Prof;
                            break;
                        }
                    }
                    if (id != FTK_proficiencyTable.ID.None)
                    {
                        acc = stats.GetSkillValue(entry._skilltest, true, FTK_proficiencyTableDB.GetDB().GetEntry(id).m_PerSlotSkillRoll);
                    }
                    break;
                default:
                    break;
            }
            return acc;
        }

        public static int GetAttackDamage(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> _Proficiencies)
        {
            int dmg = GameLogic.Instance.GetCurrentCombatCOW().m_CharacterStats.GetWeaponMaxDamage();
            FTK_proficiencyTable.ID id = FTK_proficiencyTable.ID.None;

            if (btn.m_ButtonType == uiBattleButton.BattleButtonType.proficiency)
            {
				foreach (uiBattleStanceButtons.ProfValues profValues in _Proficiencies)
				{
					if (profValues.m_Button == btn)
					{
						id = profValues.m_Prof;
						break;
					}
				}
                if (id != FTK_proficiencyTable.ID.None)
                {
                    FTK_proficiencyTable entry = FTK_proficiencyTableDB.GetDB().GetEntry(id);
                    dmg = FTKUtil.RoundToInt(dmg * entry.m_DmgMultiplier);
                }
            }
            return dmg;
        }

        private static string AddContext(Dictionary<string, Dictionary<string, string>> data, bool hasRolls = true)
        {
            string key = data.Keys.First();
            string type = data[key]["type"];
            string description = data[key]["description"];
            string rollChance = data[key]["per_roll_chance"];
            string context = $"[{key}]{type}, {description}\n";
            if (hasRolls) context = $"[{key}]{type}, {description}, success chance for each roll {rollChance}\n";
            return context;
        }

        private static string AddAttackContext(Dictionary<string, Dictionary<string, string>> data)
        {
            string key = data.Keys.First();
            string type = data[key]["type"];
            string description = data[key]["description"];
            string rollChance = data[key]["per_roll_chance"];
            string dmg = data[key]["damage"];
            string context = $"[{key}]damage: {dmg}, {type}, {StringReplace.RemoveStyling(description)}, success chance for each roll {rollChance}\n";
            return context;
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

#endregion

    }
}