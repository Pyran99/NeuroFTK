using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK
{
    public class CombatActions
    {
        public static ActionWindow RegisterActions(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.AddAction(new CombatAttackAction(instance, m_Proficiencies));
            //TODO if has friendly attacks
            window.AddAction(new CombatFriendlyAction(instance, m_Proficiencies));
            bool canFlee = instance.m_FleeButton != null && instance.m_FleeButton.isActiveAndEnabled && instance.m_FleeButton.m_CanUse;
            bool canRevive = instance.m_ReviveButton != null && instance.m_ReviveButton.isActiveAndEnabled && instance.m_ReviveButton.m_CanUse;
            bool canTaunt = instance.m_ShieldTauntButton != null && instance.m_ShieldTauntButton.isActiveAndEnabled && instance.m_ShieldTauntButton.m_CanUse;
            bool canChangeWeapon = instance.m_EquipWeaponButton != null && instance.m_EquipWeaponButton.isActiveAndEnabled && instance.m_EquipWeaponButton.m_CanUse;
            bool canHealParty = instance.m_PartyHealButton != null && instance.m_PartyHealButton.isActiveAndEnabled && instance.m_PartyHealButton.m_CanUse;
            if (canFlee) window.AddAction(new CombatFleeAction(instance.m_FleeButton));
            if (canRevive) window.AddAction(new CombatReviveAction(instance.m_ReviveButton));
            if (canTaunt) window.AddAction(new CombatTauntAction(instance.m_ShieldTauntButton));
            if (canChangeWeapon) window.AddAction(new CombatChangeWeaponAction(instance.m_EquipWeaponButton));
            if (canHealParty) window.AddAction(new CombatPartyHealAction(instance.m_PartyHealButton));
            window.SetContext("NYI combat context");
            window.SetForce(5, "choose a combat action", "NYI combat state context");
            window.Register();
            return window;
        }

        /// <summary>
        /// actions to target friendly units
        /// </summary>
        private class CombatFriendlyAction(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies) : NeuroAction
        {
            readonly Dictionary<string, uiBattleButton> btns = [];

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

            protected override void Execute()
            {
                Plugin.Logger.LogWarning("NYI combat ally");
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                Plugin.Logger.LogMessage(actionData.Data);
                return ExecutionResult.Success();
            }

            List<string> GetListOfPlayers()
            {
                List<string> names = [];
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
                if (instance.m_PartyHealButton != null && instance.m_PartyHealButton.m_CanUse) btns.Add("heal party", instance.m_PartyHealButton);
                foreach (uiBattleStanceButtons.ProfValues prof in m_Proficiencies)
                {
                    if (prof.m_Button != null && prof.m_Button.m_CanUse) btns.Add($"{prof.m_Prof}", prof.m_Button);
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
        private class CombatAttackAction(uiBattleStanceButtons instance, List<uiBattleStanceButtons.ProfValues> m_Proficiencies) : NeuroAction
        {
            readonly Dictionary<string, uiBattleButton> btns = [];
            
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
                        ["ability"] = QJS.Enum(GetListOfButtons().Keys)
                    }
                };
                return schema;
            }

            protected override void Execute()
            {
                Plugin.Logger.LogWarning("NYI combat enemy");
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                Plugin.Logger.LogMessage(actionData.Data);
                return ExecutionResult.Success();
            }

            //TODO get enemy name as displayed on ui: Enemy 1 (Timberwolf)
            List<string> GetListOfEnemies()
            {
                Dictionary<FTKPlayerID, EnemyDummy> enemies = new(EncounterSession.Instance.m_EnemyDummies);
                List<string> names = [];
                foreach (var enemy in enemies)
                {
                    names.Add(enemy.Value.GetEnemyInfo().GameLogID + ", ");
                }
                Plugin.Logger.LogMessage($"enemies [{string.Join(", ", [.. names.Select(v => v)])}] ");
                return names;
            }

            Dictionary<string, uiBattleButton> GetListOfButtons()
            {
                if (instance.m_AttackButton != null && instance.m_AttackButton.m_CanUse) btns.Add("attack", instance.m_AttackButton); // no regular atk btn
                if (instance.m_ReloadButton != null && instance.m_ReloadButton.m_CanUse) btns.Add("reload gun", instance.m_ReloadButton);
                foreach (uiBattleStanceButtons.ProfValues prof in m_Proficiencies)
                {
                    if (prof.m_Button != null && prof.m_Button.m_CanUse) btns.Add($"{prof.m_Prof}", prof.m_Button);
                }
                Plugin.Logger.LogMessage($"enemy attacks [{string.Join(", ", [.. btns.Keys.Select(v => v)])}]");
                return btns;
            }
        }

        /// <summary>
        /// try to flee combat
        /// </summary>
        private class CombatFleeAction(uiBattleButton btn): NeuroAction
        {
            public override string Name => "flee_combat";
            protected override string Description => "try to run away from combat";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                Plugin.Logger.LogWarning("NYI flee action");
                btn.OnPointerEnter(null);
                // btn.Select();
                // btn.OnControllerClick();
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                return ExecutionResult.Success();
            }
        }

        /// <summary>
        /// try to revive a character
        /// </summary>
        private class CombatReviveAction(uiBattleButton btn): NeuroAction
        {
            public override string Name => "revive_ally";
            protected override string Description => "revive a fallen party member";
            protected override JsonSchema Schema => null;

            private JsonSchema GetSchema()
            {
                // if multiple targets?
                JsonSchema schema = new();
                return schema;
            }

            protected override void Execute()
            {
                Plugin.Logger.LogWarning("NYI revive ally");
                btn.Select();
                // btn.OnControllerClick();
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                // Plugin.Logger.LogMessage(actionData.Data);
                return ExecutionResult.Success();
            }
        }

        /// <summary>
        /// force enemies to attack this unit
        /// </summary>
        private class CombatTauntAction(uiBattleButton btn) : NeuroAction
        {
            public override string Name => "taunt";
            protected override string Description => "force enemies to attack this unit";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                Plugin.Logger.LogWarning("execute taunt action");
                btn.Select();
                // btn.OnControllerClick();
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
        private class CombatChangeWeaponAction(uiBattleButton btn): NeuroAction<string>
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
                btn.Select();
                // btn.OnControllerClick();
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
        private class CombatPartyHealAction(uiBattleButton btn): NeuroAction
        {
            public override string Name => "party_heal";
            protected override string Description => "heal all party members";
            protected override JsonSchema Schema => null;

            protected override void Execute()
            {
                Plugin.Logger.LogWarning("execute party heal action");
                btn.Select();
                // btn.OnControllerClick();
            }

            protected override ExecutionResult Validate(ActionJData actionData)
            {
                return ExecutionResult.Success();
            }
        }

    }
}