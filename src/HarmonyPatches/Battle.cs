using System.Collections;
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
using System.Text;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class Battle
    {

        public static uiBattleStanceButtons StanceBtnInstance;
        public static List<uiBattleStanceButtons.ProfValues> m_Proficiencies = [];
        static ActionWindow window;
        static Dictionary<string, uiBattleButton> offense = [];
        static Dictionary<string, uiBattleButton> defense = [];
        static readonly Dictionary<string, int> playerHealths = [];
        static StringBuilder dmgTakenString = new();
        static bool isHealthChangeWait = false;
        public static bool beltActionUsed = false;


        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.StartEncounterSession))]
        [HarmonyPrefix]
        static void EnteredBattle()
        {
            ToggleDisposableActions.ToggleOverworldActions(false);
            Plugin.Logger.LogMessage("40 StartEncounterSession");
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CommenceBattleRPC))]
        [HarmonyPostfix]
        static void BeginBattle()
        {
            Plugin.Logger.LogMessage("39 CommenceBattleRPC");
            Context.Send("starting a battle", true);
        }

        #region Player

        [HarmonyPatch(typeof(uiBattleStanceButtons), nameof(uiBattleStanceButtons.Initialize))]
        [HarmonyPostfix]
        static void ButtonsInitialized(uiBattleStanceButtons __instance)
        {
            GlobalConfig.gameInitialized = true;
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogError("wrong mode for battle");
                return;
            }
            if (!Multiplayer.IsYourCow(__instance.CombatCow))
            {
                Multiplayer.SendOtherPlayerTurnCtx();
                return;
            }
            StanceBtnInstance = __instance;
            beltActionUsed = false;
            BeginTurns.CtxCombatTurnBeginEnemy();
            BeginTurns.CtxCombatTurnBeginPlayer(__instance.CombatCow);
            CreateActionWindow(StanceBtnInstance, m_Proficiencies);
            ToggleDisposableActions.ToggleCombatActions(true, false);
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
            UnregisterDisposableActions();
            m_Proficiencies = [];
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerVictory))]
        [HarmonyPrefix]
        static void CombatPlayerVictory()
        {
            UnregisterDisposableActions();
            ToggleDisposableActions.ToggleCombatActions(false);
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Overworld) // changed before post-call
            {
                Plugin.Logger.LogMessage("combat victory overworld skip");
                return;
            }
            Context.Send(StringMessages.BattleWon);
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerDie))]
        [HarmonyPostfix]
        static void PlayerDied(FTKPlayerID _victim, FTKPlayerID _attacker)
        {
            FTKPlayerID ph = _victim;
            string victim = CharacterData.GetCharacterName(ph.GetCow());
            Context.Send(StringMessages.UnitDied.Format(victim));
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatPlayerFlee))]
        [HarmonyPostfix]
        static void CombatPlayerFled(FTKPlayerID _fid)
        {
            FTKPlayerID ph = _fid;
            string player = CharacterData.GetCharacterName(ph.GetCow());
            Context.Send(StringMessages.UnitFled.Format(player));
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetSpecificHealthRPC))]
        [HarmonyPrefix]
        static void PreHealthChanged(CharacterStats __instance)
        {
            playerHealths[__instance.m_CharacterName] = __instance.m_HealthCurrent;
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetSpecificHealthRPC))] // player dmg taken // secondarydmg calls rpc directly | players only
        [HarmonyPostfix]
        static void PostHealthChanged(CharacterStats __instance)
        {
            int old = playerHealths[__instance.m_CharacterName];
            int dif = old - __instance.m_HealthCurrent;
            PlayerHealthChange(__instance.m_CharacterOverworld, dif);

            // DummyDamageInfo dmg = __instance.m_CharacterOverworld.m_CurrentDummy.m_DamageInfo;
            // int dif = dmg.m_Damage;
        }

        static Dictionary<string, string> levelUps = [];
        static bool isLevelUpWait = false;

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TallyCharacterHealth))] // called twice
        [HarmonyPostfix]
        static void PlayerLeveled(CharacterStats __instance)
        {
// level up is handled in Update
// this.TallyCharacterHealth(this.m_PlayerLevel, false, false);
// this.m_HealthCurrent = this.MaxHealth - FTKUtil.RoundToInt((float)num2 * GameFlow.Instance.GameDif.m_LevelUpHealthDifference);
// this.TallyCharacterHealth(this.m_PlayerLevel, true, false);
            int level = __instance.m_PlayerLevel;
            if (level == 0) return;
            string name = __instance.m_CharacterName;
            if (!playerHealths.ContainsKey(name) || playerHealths[name] == __instance.m_HealthCurrent) return;
            playerHealths[name] = __instance.m_HealthCurrent;
            string ctx = $"{name} leveled up to {level}! health {__instance.GetHealthDisplayString()}";
            levelUps[name] = ctx;
            // Context.Send(ctx);
            if (isLevelUpWait) return;
            GameLogic.Instance.StartCoroutine(LevelUpWait());
        }
        
        static IEnumerator LevelUpWait()
        {
            isLevelUpWait = true;
            yield return new WaitForEndOfFrame();
            isLevelUpWait = false;
            StringBuilder sb = new();
            foreach (KeyValuePair<string, string> kvp in levelUps)
            {
                sb.AppendLine(kvp.Value);
            }
            Context.Send(sb.ToString());
            levelUps = [];
        }

        static void PlayerHealthChange(CharacterOverworld character, int change)
        {
            string name = CharacterData.GetCharacterName(character);
            string health = CharacterData.GetCharacterHealth(character);
            if (change >= 0)
            {
                // dmgTakenString.AppendLine($"{name} took {change} damage (health {health})");
                dmgTakenString.AppendLine(StringMessages.UnitTakeDamage.Format([name, change, health]));
            }
            else if (change < 0)
            {
                // dmgTakenString.AppendLine($"{name} healed {-change} (health {health})");
                dmgTakenString.AppendLine(StringMessages.UnitHealed.Format([name, -change, health]));
            }
            if (isHealthChangeWait) return;
            GameLogic.Instance.StartCoroutine(PlayerHealthWait());
        }

        static IEnumerator PlayerHealthWait()
        {
            isHealthChangeWait = true;
            yield return new WaitForEndOfFrame();
            Context.Send(dmgTakenString.ToString());
            dmgTakenString = new();
            isHealthChangeWait = false;
        }


        #endregion



        #region Enemy

        static readonly Dictionary<FTKPlayerID, int> enemyHealths = [];
        static bool isWaitingEnemyHealth = false;
        static StringBuilder enemySb = new();

        static StringBuilder enemyDiedSB = new();
        static bool isEnemyDeathWait = false;

        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.InitEnemyDummiesForCombat))]
        [HarmonyPostfix]
        static void InitEnemyDummies()
        {
            foreach (KeyValuePair<FTKPlayerID, EnemyDummy> dummy in EncounterSession.Instance.m_EnemyDummies)
            {
                enemyHealths[dummy.Key] = dummy.Value.m_CurrentHealth;
            }
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyFlee))]
        [HarmonyPostfix]
        static void EnemyFled(FTKPlayerID _enemyID)
        {
            CharacterDummy dummy = EncounterSession.Instance.GetDummyByFID(_enemyID);
            if (dummy == null) return;
            string enemy = CombatUtils.GetEnemyName(dummy as EnemyDummy);
            Context.Send(StringMessages.UnitFled.Format(enemy));
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyDie))]
        [HarmonyPostfix]
        static void EnemyDied(FTKPlayerID _victim, FTKPlayerID _attacker)
        {
            CharacterDummy dummy = EncounterSession.Instance.GetDummyByFID(_victim);
            if (dummy == null) return;
            enemyDiedSB.AppendLine(StringMessages.UnitDied.Format(CombatUtils.GetEnemyName(dummy as EnemyDummy)));
            if (isEnemyDeathWait) return;
            EncounterSession.Instance.StartCoroutine(EnemyDiedWait());
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.CombatEnemyBlackHoled))]
        [HarmonyPostfix]
        static void CombatEnemyBlackHoled(FTKPlayerID _enemyID)
        {
            CharacterDummy dummy = EncounterSession.Instance.GetDummyByFID(_enemyID);
            if (dummy == null)
            {
                Plugin.Logger.LogError("CombatEnemyBlackHoled null dummy");
                return;
            }
            Context.Send($"[enemy] {CombatUtils.GetEnemyName(dummy as EnemyDummy)} was consumed by a black hole");
        }

        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.UpdateEnemyHealthRPC))]
        [HarmonyPostfix]
        static void UpdateEnemyHealthPost(FTKPlayerID _enemyID, int _newHealth)
        {
            int oldHealth = enemyHealths[_enemyID];
            int dif = oldHealth - _newHealth;
            string name = CombatUtils.GetEnemyName(EncounterSession.Instance.m_EnemyDummies[_enemyID]);
            if (dif >= 0)
            {
                enemySb.AppendLine(StringMessages.UnitTakeDamage.Format([name, dif, _newHealth]));
            }
            else
            {
                enemySb.AppendLine(StringMessages.UnitHealed.Format([name, dif, _newHealth]));
            }
            enemyHealths[_enemyID] = _newHealth;
            if (isWaitingEnemyHealth) return;
            EncounterSession.Instance.StartCoroutine(EnemyHealthWait());
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

        static IEnumerator EnemyDiedWait()
        {
            isEnemyDeathWait = true;
            yield return new WaitForEndOfFrame();
            Context.Send(enemyDiedSB.ToString());
            enemyDiedSB = new();
            isEnemyDeathWait = false;
        }

        static IEnumerator EnemyHealthWait()
        {
            isWaitingEnemyHealth = true;
            yield return new WaitForEndOfFrame();
            Context.Send(enemySb.ToString());
            enemySb = new();
            isWaitingEnemyHealth = false;
        }


        #endregion



        #region action window

        static readonly List<INeuroAction> actions = [];
        static readonly List<INeuroAction> disposableActions = [];

        public static void CreateActionWindow(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> _proficiencies)
        {
            Object.Destroy(window);
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCombatCOW();
            GetOffenseAttackDetails(_instance, _proficiencies);
            GetDefenseAttackDetails(_instance, _proficiencies);
            actions.Clear();
            string ctx = GetAttackContextAndRegisterAction(_instance, _proficiencies);
            ctx += GetBeltDetails(cow);
            // RegisterDisposableActions(cow);
            if (!beltActionUsed) // unsure where to put belt use
            {
                beltActionUsed = true;
                Dictionary<string, FTK_itembase.ID> items = [];
                foreach (FTK_itembase.ID item in ItemData.GetUsableBeltItems(cow)) items.Add(ItemData.GetItemName(item), item);
                if (items.Count > 0) actions.Add(new UseBeltItemAction(items, cow, false, true));
            }
            window = CombatActions.RegisterCombatActions(_instance, ctx, actions);
            offense.Clear();
            defense.Clear();
        }

        static void RegisterDisposableActions(CharacterOverworld cow)
        {
            Dictionary<string, FTK_itembase.ID> items = [];
            foreach (FTK_itembase.ID item in ItemData.GetUsableBeltItems(cow))
            {
                items.Add(ItemData.GetItemName(item), item);
            }
            disposableActions.Clear();
            disposableActions.Add(new SillyAction());
            if (items.Count > 0)
            {
                disposableActions.Add(new UseBeltItemAction(items, cow));
            }
            NeuroActionHandler.RegisterActions(disposableActions);
        }

        static void UnregisterDisposableActions()
        {
            if (disposableActions.Count == 0) return;
            NeuroActionHandler.UnregisterActions(disposableActions);
            disposableActions.Clear();
        }

        public static string GetBeltDetails(CharacterOverworld cow)
        {
            StringBuilder sb = new();
            List<FTK_itembase.ID> items = ItemData.GetUsableBeltItems(cow);
            if (items.Count == 0) return "";
            sb.Append("\n[usable belt items] ");
            foreach (FTK_itembase.ID item in items)
            {
                sb.AppendLine($"({ItemData.GetItemName(item)}){ItemData.GetItemDescription(item, true, cow)}");
            }
            return sb.ToString();
        }

        static string GetAttackContextAndRegisterAction(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> _proficiencies)
        {
            StringBuilder sb = new();
            sb.Append("[your attacks] ");
            if (offense.Count > 0)
            {
                foreach (string key in offense.Keys)
                {
                    var data = GetActionDetails(offense[key], _proficiencies);
                    sb.Append(AddAttackContext(data));
                }
                actions.Add(new CombatAttackAction(offense));
            }
            if (defense.Count > 0)
            {
                foreach (string key in defense.Keys)
                {
                    var data = GetActionDetails(defense[key], _proficiencies);
                    sb.Append(AddAttackContext(data));
                }
                actions.Add(new CombatFriendlyAction(defense));
            }
            if (CanUseBtn(_instance.m_FleeButton) && !GlobalConfig.IsDebugMode())
            {
                sb.Append(HandleBtnContext(_instance.m_FleeButton, _proficiencies));
                actions.Add(new CombatFleeAction(_instance.m_FleeButton));
            } 
            if (CanUseBtn(_instance.m_ReviveButton))
            {
                sb.Append(HandleBtnContext(_instance.m_ReviveButton, _proficiencies, false));
                actions.Add(new CombatReviveAction(_instance.m_ReviveButton));
            }
            if (CanUseBtn(_instance.m_ShieldTauntButton))
            {
                sb.Append(HandleBtnContext(_instance.m_ShieldTauntButton, _proficiencies));
                actions.Add(new CombatTauntAction(_instance.m_ShieldTauntButton));
            }
            if (CanUseBtn(_instance.m_EquipWeaponButton) && !GlobalConfig.IsDebugMode())
            {
                sb.Append(HandleBtnContext(_instance.m_EquipWeaponButton, _proficiencies, false));
                actions.Add(new CombatChangeWeaponAction(_instance.m_EquipWeaponButton));
            }
            if (CanUseBtn(_instance.m_PartyHealButton))
            {
                sb.Append(HandleBtnContext(_instance.m_PartyHealButton, _proficiencies, false));
                actions.Add(new CombatPartyHealAction(_instance.m_PartyHealButton));
            }
            return sb.ToString();
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
            if (hasRolls) context = $"({key}) {type}, {description}, success chance for each roll {rollChance}\n";
            return context;
        }

        private static string AddAttackContext(Dictionary<string, Dictionary<string, string>> data)
        {
            string key = data.Keys.First();
            string type = data[key]["type"];
            string description = data[key]["description"];
            string rollChance = data[key]["per_roll_chance"];
            string dmg = data[key]["damage"];
            string context = $"({key}) damage: {dmg}, {type}, {StringReplace.RemoveStyling(description)}, success chance for each roll {rollChance}\n";
            return context;
        }

        #endregion

    }
}