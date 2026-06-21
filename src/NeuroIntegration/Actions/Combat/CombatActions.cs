using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK
{
    public class CombatActions
    {
        static int maxRolls; // NYI

        public static ActionWindow RegisterActions(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            string context = "";
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            GetOffenseAttackDetails(instance, m_Proficiencies);
            GetDefenseAttackDetails(instance, m_Proficiencies);
            window.AddAction(new CombatAttackAction(instance, m_Proficiencies));
            //TODO if has friendly attacks
            window.AddAction(new CombatFriendlyAction(instance, m_Proficiencies));
            bool canFlee = instance.m_FleeButton != null && instance.m_FleeButton.isActiveAndEnabled && instance.m_FleeButton.m_CanUse;
            bool canRevive = instance.m_ReviveButton != null && instance.m_ReviveButton.isActiveAndEnabled && instance.m_ReviveButton.m_CanUse;
            bool canTaunt = instance.m_ShieldTauntButton != null && instance.m_ShieldTauntButton.isActiveAndEnabled && instance.m_ShieldTauntButton.m_CanUse;
            bool canChangeWeapon = instance.m_EquipWeaponButton != null && instance.m_EquipWeaponButton.isActiveAndEnabled && instance.m_EquipWeaponButton.m_CanUse;
            bool canHealParty = instance.m_PartyHealButton != null && instance.m_PartyHealButton.isActiveAndEnabled && instance.m_PartyHealButton.m_CanUse;
            if (canFlee)
            {
                var data = GetActionDetails(instance.m_FleeButton, m_Proficiencies);
                context += AddContext(data);
                Plugin.Logger.LogMessage(context);
                window.AddAction(new CombatFleeAction(instance, instance.m_FleeButton));
            } 
            if (canRevive)
            {
                var data = GetActionDetails(instance.m_ReviveButton, m_Proficiencies);
                context += AddContext(data, false);
                Plugin.Logger.LogMessage(context);
                window.AddAction(new CombatReviveAction(instance, instance.m_ReviveButton));
            }
            if (canTaunt)
            {
                var data = GetActionDetails(instance.m_ShieldTauntButton, m_Proficiencies);
                context += AddContext(data);
                Plugin.Logger.LogMessage(context);
                window.AddAction(new CombatTauntAction(instance, instance.m_ShieldTauntButton));
            }
            if (canChangeWeapon)
            {
                var data = GetActionDetails(instance.m_EquipWeaponButton, m_Proficiencies);
                context += AddContext(data, false);
                Plugin.Logger.LogMessage(context);
                window.AddAction(new CombatChangeWeaponAction(instance, instance.m_EquipWeaponButton));
            }
            if (canHealParty)
            {
                var data = GetActionDetails(instance.m_PartyHealButton, m_Proficiencies);
                context += AddContext(data, false);
                Plugin.Logger.LogMessage(context);
                window.AddAction(new CombatPartyHealAction(instance, instance.m_PartyHealButton));
            }
            window.SetContext(context);
            window.SetForce(5, "choose a combat action", "NYI combat state context");
            window.Register();
            return window;
        }

        private static string AddContext(Dictionary<string, Dictionary<string, string>> data, bool hasRolls = true)
        {
            string key = data.Keys.First();
            string type = data[key]["type"];
            string description = data[key]["description"];
            string rollChance = data[key]["per_roll_chance"];
            string context = $"[[{key}][{type}][{description}]]\n";
            if (hasRolls) context = $"[[{key}][{type}][{description}][success chance for each roll slot {rollChance}]\n";
            return context;
        }

        private static string AddAttackContext(Dictionary<string, Dictionary<string, string>> data)
        {
            string key = data.Keys.First();
            string type = data[key]["type"];
            string description = data[key]["description"];
            string rollChance = data[key]["per_roll_chance"];
            string dmg = data[key]["damage"];
            string context = $"[[{key}][damage: {dmg}][{type}][{StringReplace.RemoveStyling(description)}][success chance for each roll slot {rollChance}]\n";
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
            data[text]["per_roll_chance"] = "";
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


        static Dictionary<string, uiBattleButton> offense = [];

        public static void GetOffenseAttackDetails(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            Dictionary<string, uiBattleButton> btns = [];
            // var data = GetActionDetails(instance.m_PartyHealButton, m_Proficiencies);
            // context += AddContext(data, false);
            // Plugin.Logger.LogMessage(context);
            bool useDefault = instance.m_AttackButton != null && instance.m_AttackButton.m_CanUse && instance.m_AttackButton.isActiveAndEnabled;
            bool canReload = instance.m_ReloadButton != null && instance.m_ReloadButton.m_CanUse && instance.m_ReloadButton.isActiveAndEnabled;
            if (useDefault)
            {
                Plugin.Logger.LogMessage(AddAttackContext(GetActionDetails(instance.m_AttackButton, m_Proficiencies)));
                btns.Add("attack", instance.m_AttackButton);
            }
            if (canReload)
            {
                Plugin.Logger.LogMessage(AddAttackContext(GetActionDetails(instance.m_ReloadButton, m_Proficiencies)));
                btns.Add("reload gun", instance.m_ReloadButton);
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
                string name = entry.GetLocalizedDisplayTitle();
                if (prof.m_Button != null && prof.m_Button.m_CanUse && prof.m_Button.isActiveAndEnabled) btns.Add($"{name}", prof.m_Button);
                var data = GetActionDetails(prof.m_Button, m_Proficiencies);
                Plugin.Logger.LogMessage(AddAttackContext(data));
            }
            offense = new Dictionary<string, uiBattleButton>(btns);
        }

        //TODO
        public static void GetDefenseAttackDetails(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            // var data = GetActionDetails(instance.m_PartyHealButton, m_Proficiencies);
            // context += AddContext(data, false);
            // Plugin.Logger.LogMessage(context);
            
        }




#region Actions

        /// <summary>
        /// actions to target friendly units
        /// </summary>
        private class CombatFriendlyAction(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies) : NeuroAction<string[]>
        {
            readonly Dictionary<string, uiBattleButton> btns = [];
            List<string> names = [];

            public override string Name => "ally_target";
            protected override string Description => "heal or buff an ally";
            protected override JsonSchema Schema => GetSchema();

            private JsonSchema GetSchema()
            {
                JsonSchema schema = new()
                {
                    Type = JsonSchemaType.Object,
                    Required = ["target", "ability"],
                    Properties = new()
                    {
                        ["target"] = QJS.Enum(GetListOfPlayers()),
                        ["ability"] = QJS.Enum(GetListOfButtons().Keys)
                    }
                };
                return schema;
            }

            protected override void Execute(string[] parsedData)
            {
                Plugin.Logger.LogMessage("execute attack: " + string.Concat(parsedData));
                //TODO pick target
                btns.TryGetValue(parsedData[1], out uiBattleButton btn);
                SelectButton.StartCoroutine(instance, btn, 1.0f);
            }

            protected override ExecutionResult Validate(ActionJData actionData, out string[] parsedData)
            {
                parsedData = new string[2];
                Plugin.Logger.LogMessage(actionData.Data);
                string target = actionData.Data.Value<string>("target");
                string ability = actionData.Data.Value<string>("ability");
                if (!names.Contains(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
                if (!btns.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
                parsedData[0] = target;
                parsedData[1] = ability;
                return ExecutionResult.Success();
            }
//TODO maybe move these gets to static var for context usage
            List<string> GetListOfPlayers()
            {
                names = [];
                Dictionary<FTKPlayerID, CharacterDummy> players = new(EncounterSession.Instance.m_PlayerDummies);
                foreach (var player in players)
                {
                    names.Add(player.Value.m_CharacterOverworld.m_CharacterStats.m_CharacterName + ", ");
                }
                Plugin.Logger.LogMessage($"players [{string.Join(", ", [.. names.Select(v => v)])}] ");
                return names;
            }

            Dictionary<string, uiBattleButton> GetListOfButtons()
            {
                btns.Clear();
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
                    string name = entry.GetLocalizedDisplayTitle();
                    if (prof.m_Button != null && prof.m_Button.m_CanUse) btns.Add($"{name}", prof.m_Button);
                }
                Plugin.Logger.LogMessage($"player attacks [{string.Join(", ", [.. btns.Keys.Select(v => v)])}]");
                return btns;
            }

            // players only?
            List<string> GetAll()
            {
                FTKPlayerID[] allCombatants = [.. EncounterSessionMC.Instance.m_AllCombtatants];
                List<string> names = [];
                foreach (var combatant in allCombatants)
                {
                    CharacterOverworld cow = combatant.GetCow();
                    if (cow == null)
                    {
                        Plugin.Logger.LogWarning(cow);
                        continue;
                    }
                    names.Add(cow.m_CharacterStats.m_CharacterName + ", ");
                }
                Plugin.Logger.LogMessage(names.Select(v => v).ToString());
                return names;
            }
        }

        /// <summary>
        /// actions to target enemies
        /// </summary>
        private class CombatAttackAction(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies) : NeuroAction<string[]>
        {
            List<string> names = [];
            
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
                        ["target"] = QJS.Enum(GetListOfEnemies()),
                        // ["ability"] = QJS.Enum(GetListOfButtons().Keys),
                        ["ability"] = QJS.Enum(offense.Keys)
                    }
                };
                return schema;
            }

            protected override void Execute(string[] parsedData)
            {
                Plugin.Logger.LogMessage("execute attack: " + string.Concat(parsedData));
                //TODO pick target
                offense.TryGetValue(parsedData[1], out uiBattleButton btn);
                SelectButton.StartCoroutine(instance, btn, 1.0f);
            }

            protected override ExecutionResult Validate(ActionJData actionData, out string[] parsedData)
            {
                parsedData = new string[2];
                Plugin.Logger.LogMessage(actionData.Data);
                string target = actionData.Data.Value<string>("target");
                string ability = actionData.Data.Value<string>("ability");
                if (!names.Contains(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
                if (!offense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
                parsedData[0] = target;
                parsedData[1] = ability;
                return ExecutionResult.Success();
            }

            //TODO get enemy name as displayed on ui: Enemy 1 (Timberwolf)
            List<string> GetListOfEnemies()
            {
                names = [];
                Dictionary<FTKPlayerID, EnemyDummy> enemies = new(EncounterSession.Instance.m_EnemyDummies);
                foreach (var enemy in enemies)
                {
                    names.Add(enemy.Value.GetEnemyInfo().GameLogID + ", ");
                }
                Plugin.Logger.LogMessage($"enemies [{string.Join(", ", [.. names.Select(v => v)])}] ");
                return names;
            }

            Dictionary<string, uiBattleButton> GetListOfButtons()
            {
                // btns.Clear();
                // bool useDefault = instance.m_AttackButton != null && instance.m_AttackButton.m_CanUse && instance.m_AttackButton.isActiveAndEnabled;
                // bool canReload = instance.m_ReloadButton != null && instance.m_ReloadButton.m_CanUse && instance.m_ReloadButton.isActiveAndEnabled;
                // if (useDefault) btns.Add("attack", instance.m_AttackButton);
                // if (canReload) btns.Add("reload gun", instance.m_ReloadButton);
                // foreach (uiBattleStanceButtons.ProfValues prof in m_Proficiencies)
                // {
                //     switch (prof.m_Button.m_ButtonType)
                //     {
                //         case uiBattleButton.BattleButtonType.flee:
                //         case uiBattleButton.BattleButtonType.shieldtaunt:
                //         case uiBattleButton.BattleButtonType.equipweapon:
                //         case uiBattleButton.BattleButtonType.revive:
                //         case uiBattleButton.BattleButtonType.partyheal:
                //             continue;
                //     }
                //     if (prof.m_Prof == FTK_proficiencyTable.ID.None) continue;
                //     FTK_proficiencyTable entry = FTK_proficiencyTableDB.GetDB().GetEntry(prof.m_Prof);
                //     string name = entry.GetLocalizedDisplayTitle();
                //     if (prof.m_Button != null && prof.m_Button.m_CanUse && prof.m_Button.isActiveAndEnabled) btns.Add($"{name}", prof.m_Button);
                // }
                // Plugin.Logger.LogMessage($"enemy attacks [{string.Join(", ", [.. btns.Keys.Select(v => v)])}]");
                // return btns;
                return [];
            }
        }

        /// <summary>
        /// try to flee combat
        /// </summary>
        private class CombatFleeAction(uiBattleStanceButtons instance, uiBattleButton btn): NeuroAction
        {
            public override string Name => "flee_combat";
            protected override string Description => "try to run away from combat. only the character this is used with will exit combat.";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                SelectButton.StartCoroutine(instance, btn, 1.0f);
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                return ExecutionResult.Success();
            }
        }

        /// <summary>
        /// try to revive a character
        /// </summary>
        private class CombatReviveAction(uiBattleStanceButtons instance, uiBattleButton btn): NeuroAction
        {
            public override string Name => "revive_ally";
            protected override string Description => "revive a fallen party member";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                SelectButton.StartCoroutine(instance, btn, 1.0f);
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                return ExecutionResult.Success();
            }
        }

        /// <summary>
        /// force enemies to attack this unit
        /// </summary>
        private class CombatTauntAction(uiBattleStanceButtons instance, uiBattleButton btn) : NeuroAction
        {
            public override string Name => "taunt";
            protected override string Description => "force enemies to attack this unit";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                SelectButton.StartCoroutine(instance, btn, 1.0f);
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                return ExecutionResult.Success();
            }
        }

        /// <summary>
        /// switch to another weapon
        /// TODO get weapons from inventory
        /// </summary>
        private class CombatChangeWeaponAction(uiBattleStanceButtons instance, uiBattleButton btn): NeuroAction<string>
        {
            public override string Name => "change_weapon";
            protected override string Description => "equip a different weapon. this will end your turn";
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
                Plugin.Logger.LogWarning("execute change weapon action " + parsedData);
                SelectButton.StartCoroutine(instance, btn, 1.0f);
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
        private class CombatPartyHealAction(uiBattleStanceButtons instance, uiBattleButton btn): NeuroAction
        {
            public override string Name => "party_heal";
            protected override string Description => "heal all party members";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                SelectButton.StartCoroutine(instance, btn, 1.0f);
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                return ExecutionResult.Success();
            }
        }

#endregion

    }


}