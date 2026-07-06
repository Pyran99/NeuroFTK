using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{

#region Actions

    /// <summary>
    /// actions to target friendly units
    /// </summary>
    public class CombatFriendlyAction(Dictionary<string, uiBattleButton> _defense) : NeuroAction<object[]>
    {
        Dictionary<FTKPlayerID, string> names = [];

        public override string Name => "ally_target";
        protected override string Description => "heal or buff an ally or self";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["target", "ability"],
                Properties = new()
                {
                    ["target"] = QJS.Enum(GetListOfPlayers().Values),
                    ["ability"] = QJS.Enum(_defense.Keys)
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            Plugin.Logger.LogMessage("execute attack: " + string.Concat(parsedData));
            _defense.TryGetValue((string)parsedData[1], out uiBattleButton btn);
            FTKPlayerID target = names.First(v => v.Value == (string)parsedData[0]).Key;
            if (target == null)
            {
                Plugin.Logger.LogError("target is null " + parsedData[0]);
                return;
            }
            btn.OnPointerEnter(null); // may be needed to allow friendly targeting?
            CombatActions.SelectTarget(target);
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new string[2];
            string target = actionData.Data.Value<string>("target");
            if (!names.ContainsValue(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
            string ability = actionData.Data.Value<string>("ability");
            if (!_defense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
            parsedData[0] = target;
            parsedData[1] = ability;
            return ExecutionResult.Success();
        }

        Dictionary<FTKPlayerID, string> GetListOfPlayers()
        {
            names = [];
            Dictionary<FTKPlayerID, CharacterDummy> players = new(EncounterSession.Instance.m_PlayerDummies);
            // EncounterSessionMC.Instance.m_AllCombtatantsAlive // used by enemy targeting
            foreach (var player in players)
            {
                names.Add(player.Key, player.Value.m_CharacterOverworld.m_CharacterStats.m_CharacterName);
            }
            return names;
        }
    }

    /// <summary>
    /// actions to target enemies
    /// </summary>
    public class CombatAttackAction(Dictionary<string, uiBattleButton> _offense) : NeuroAction<object[]>
    {
        Dictionary<FTKPlayerID, string> names = [];
        
        public override string Name => "attack_enemy";
        protected override string Description => "attack an enemy";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["target", "ability"],
                Properties = new()
                {
                    ["target"] = QJS.Enum(GetListOfEnemies().Values),
                    ["ability"] = QJS.Enum(_offense.Keys)
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            Plugin.Logger.LogMessage("execute attack: " + string.Join(", ", (string[])parsedData));
            // Plugin.Logger.LogMessage("execute attack: " + string.Concat(parsedData));
            _offense.TryGetValue((string)parsedData[1], out uiBattleButton btn);
            FTKPlayerID target = names.First(v => v.Value == (string)parsedData[0]).Key;
            if (target == null)
            {
                Plugin.Logger.LogError("target is null " + parsedData[0]);
                return;
            }
            CombatActions.SelectTarget(target);
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new string[2];
            string target = actionData.Data.Value<string>("target");
            if (!names.ContainsValue(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
            string ability = actionData.Data.Value<string>("ability");
            if (!_offense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
            parsedData[0] = target;
            parsedData[1] = ability;
            return ExecutionResult.Success();
        }

        Dictionary<FTKPlayerID, string> GetListOfEnemies()
        {
            names = [];
            Dictionary<FTKPlayerID, EnemyDummy> enemies = new(EncounterSession.Instance.m_EnemyDummies);
            int count = 0;
            foreach (var enemy in enemies)
            {
                if (!enemy.Value.m_IsAlive) continue;
                string name = $"{enemy.Value.GetEnemyInfo().m_EnemyCombat.GetEnemyDisplay()}"; // Timberwolf 1
                if (names.ContainsValue(name))
                {
                    name += $" {++count}";
                }
                names.Add(enemy.Key, name);
            }
            return names;
        }
    }

    /// <summary>
    /// try to flee combat
    /// </summary>
    public class CombatFleeAction(uiBattleButton btn): NeuroAction
    {
        public override string Name => "flee_combat";
        protected override string Description => "try to run away from combat. only the character this is used with will exit combat.";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// try to revive a character
    /// </summary>
    public class CombatReviveAction(uiBattleButton btn): NeuroAction
    {
        public override string Name => "revive_ally";
        protected override string Description => "revive a fallen party member";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// force enemies to attack this unit
    /// </summary>
    public class CombatTauntAction(uiBattleButton btn) : NeuroAction
    {
        public override string Name => "taunt";
        protected override string Description => "try to force enemies to attack this unit";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// change weapon
    /// TODO get & send context of available weapons
    /// </summary>
    public class CombatChangeWeaponAction(uiBattleButton btn): NeuroAction<string>
    {
        public override string Name => "change_weapon";
        protected override string Description => "equip a different weapon. this will also end your turn";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["weapon"],
                Properties = new()
                {
                    ["weapon"] = new() { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 16 }
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogWarning("execute change weapon action: " + parsedData);
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data.Value<string>("weapon") ?? "null";
            return ExecutionResult.Success();
        }
        
    }

    /// <summary>
    /// heal all party members
    /// </summary>
    public class CombatPartyHealAction(uiBattleButton btn): NeuroAction
    {
        public override string Name => "party_heal";
        protected override string Description => "heal all party members";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

#endregion


    public class CombatActions
    {
        static int maxRolls; // NYI
        static uiBattleStanceButtons instance;
        static Dictionary<string, uiBattleButton> offense = [];
        static Dictionary<string, uiBattleButton> defense = [];

        public static ActionWindow RegisterActions(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            instance = _instance;
            string context = "";
            offense.Clear();
            defense.Clear();
            ActionWindow window = ActionWindow.Create(_instance.gameObject);
            GetOffenseAttackDetails(_instance, m_Proficiencies);
            GetDefenseAttackDetails(_instance, m_Proficiencies);
            if (offense.Count > 0)
            {
                foreach (string key in offense.Keys)
                {
                    var data = GetActionDetails(offense[key], m_Proficiencies);
                    context += AddAttackContext(data);
                }
                window.AddAction(new CombatAttackAction(offense));
            }
            if (defense.Count > 0)
            {
                foreach (string key in defense.Keys)
                {
                    var data = GetActionDetails(defense[key], m_Proficiencies);
                    context += AddAttackContext(data);
                }
                window.AddAction(new CombatFriendlyAction(defense));
            }
            bool canFlee = _instance.m_FleeButton != null && _instance.m_FleeButton.isActiveAndEnabled && _instance.m_FleeButton.m_CanUse;
            bool canRevive = _instance.m_ReviveButton != null && _instance.m_ReviveButton.isActiveAndEnabled && _instance.m_ReviveButton.m_CanUse;
            bool canTaunt = _instance.m_ShieldTauntButton != null && _instance.m_ShieldTauntButton.isActiveAndEnabled && _instance.m_ShieldTauntButton.m_CanUse;
            bool canChangeWeapon = _instance.m_EquipWeaponButton != null && _instance.m_EquipWeaponButton.isActiveAndEnabled && _instance.m_EquipWeaponButton.m_CanUse;
            bool canHealParty = _instance.m_PartyHealButton != null && _instance.m_PartyHealButton.isActiveAndEnabled && _instance.m_PartyHealButton.m_CanUse;
            if (canFlee && !GlobalConfig.debug_mode)
            {
                var data = GetActionDetails(_instance.m_FleeButton, m_Proficiencies);
                context += AddContext(data);
                window.AddAction(new CombatFleeAction(_instance.m_FleeButton));
            } 
            if (canRevive)
            {
                var data = GetActionDetails(_instance.m_ReviveButton, m_Proficiencies);
                context += AddContext(data, false);
                window.AddAction(new CombatReviveAction(_instance.m_ReviveButton));
            }
            if (canTaunt)
            {
                var data = GetActionDetails(_instance.m_ShieldTauntButton, m_Proficiencies);
                context += AddContext(data);
                window.AddAction(new CombatTauntAction(_instance.m_ShieldTauntButton));
            }
            if (canChangeWeapon)
            {
                var data = GetActionDetails(_instance.m_EquipWeaponButton, m_Proficiencies);
                context += AddContext(data, false);
                window.AddAction(new CombatChangeWeaponAction(_instance.m_EquipWeaponButton));
            }
            if (canHealParty)
            {
                var data = GetActionDetails(_instance.m_PartyHealButton, m_Proficiencies);
                context += AddContext(data, false);
                window.AddAction(new CombatPartyHealAction(_instance.m_PartyHealButton));
            }
            window.SetContext(context);
            window.SetForce(1, "choose a combat action", "it is your turn to act");
            window.Register();
            return window;
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

        /// <summary>
        /// dictionary style: "name": {"type": "target self", "description": "perfect(56%) = leave combat", "per_roll_chance": "50", "damage": "10"}
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> GetActionDetails(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            // from => public void DisplayBattleActionInfo(uiBattleButton _button, bool _on)
            Dictionary<string, Dictionary<string, string>> data = [];
            CharacterOverworld current = GameLogic.Instance.GetCurrentCombatCOW();
            CharacterStats stats = current.m_CharacterStats;
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
                    foreach (uiBattleStanceButtons.ProfValues profValues in m_Proficiencies)
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
            maxRolls = maxRollSlots;
            data.Add(text, []);
            data[text]["type"] = array[0];
            data[text]["description"] = array[1];
            data[text]["per_roll_chance"] = "100%";
            float accuracy = GetAccuracy(btn, m_Proficiencies);
            if (accuracy > -1f) data[text]["per_roll_chance"] = FTKUtil.RoundToInt(accuracy * 100f).ToString() + "%";
            data[text]["damage"] = GetAttackDamage(btn, m_Proficiencies).ToString();
            return data;
        }

        public static int GetAttackDamage(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            int dmg = GameLogic.Instance.GetCurrentCombatCOW().m_CharacterStats.GetWeaponMaxDamage();
            FTK_proficiencyTable.ID id = FTK_proficiencyTable.ID.None;

            if (btn.m_ButtonType == uiBattleButton.BattleButtonType.proficiency)
            {
				foreach (uiBattleStanceButtons.ProfValues profValues in m_Proficiencies)
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

        public static float GetAccuracy(uiBattleButton btn, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            CharacterOverworld current = GameLogic.Instance.GetCurrentCombatCOW();
            CharacterStats stats = current.m_CharacterStats;
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
                    foreach (uiBattleStanceButtons.ProfValues profValues in m_Proficiencies)
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

        public static void GetOffenseAttackDetails(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
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
            foreach (uiBattleStanceButtons.ProfValues prof in m_Proficiencies)
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

        public static void GetDefenseAttackDetails(uiBattleStanceButtons _instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            // GetTargetInfo(FTK_proficiencyTable.ID pid, out CharacterDummy.TargetType _targetType, out bool _targetFriendly)
            Dictionary<string, uiBattleButton> btns = [];
            foreach (uiBattleStanceButtons.ProfValues prof in m_Proficiencies)
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

        public static void SelectTarget(FTKPlayerID target, FTK_itembase.ID _item = FTK_itembase.ID.None)
        {
            if (instance == null)
            {
                Plugin.Logger.LogError("battle buttons instance null");
                return;
            }
            instance.SelectEnemyDummy(target, _item);
        }



    }


}