using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{

    public class CombatActions
    {
        static uiBattleStanceButtons instance;

        public static ActionWindow RegisterCombatActions(uiBattleStanceButtons _instance, string ctx, List<INeuroAction> actions)
        {
            instance = _instance;
            if (actions.Count == 0)
            {
                Plugin.Logger.LogError("no combat actions to register");
                actions.Add(new CombatFleeAction(_instance.m_FleeButton));
            }
            ActionWindow window = ActionWindow.Create(_instance.gameObject);
            window.SetContext(ctx);
            foreach (INeuroAction action in actions)
            {
                window.AddAction(action);
            }
            window.SetForce(3, $"it is your turn with {CharacterData.GetCharacterName(instance.CombatCow)}, choose an attack action", "", true);
            window.Register();
            return window;
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


#region Actions

    /// <summary>
    /// actions to target friendly units
    /// </summary>
    public class CombatFriendlyAction(Dictionary<string, uiBattleButton> _defense) : NeuroAction<string>
    {
        readonly Dictionary<string, uiBattleButton> defense = new(_defense);

        public override string Name => "ally_target";
        protected override string Description => "heal/buff an ally or self";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["ability"],
                Properties = new()
                {
                    ["ability"] = QJS.Enum(defense.Keys),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage("execute attack: " + string.Concat(parsedData));
            defense.TryGetValue(parsedData, out uiBattleButton btn);
            ChooseRewardMenu.teamState = $"you can apply your action {parsedData} to \n" + BeginTurns.GetSimplifiedTeamState();
            SelectButton.StartCoroutine(btn, 0.5f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string ability = actionData.Data.Value<string>("ability");
            if (!defense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
            parsedData = ability;
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// actions to target enemies
    /// </summary>
    public class CombatAttackAction(Dictionary<string, uiBattleButton> _offense) : NeuroAction<object[]>
    {
        Dictionary<FTKPlayerID, string> names = [];
        readonly Dictionary<string, uiBattleButton> offense = new(_offense);
        
        public override string Name => "attack_enemy";
        protected override string Description => "choose an enemy to attack with an ability";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["target", "ability"],
                Properties = new()
                {
                    ["ability"] = QJS.Enum(offense.Keys),
                    ["target"] = QJS.Enum(GetListOfEnemies().Values)
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            Plugin.Logger.LogMessage("execute attack: " + string.Join(", ", (string[])parsedData));
            offense.TryGetValue((string)parsedData[1], out uiBattleButton btn);
            FTKPlayerID target = names.First(v => v.Value == (string)parsedData[0]).Key;
            if (target == null)
            {
                Plugin.Logger.LogError("target is null " + parsedData[0]);
                return;
            }
            btn.OnPointerEnter(null);
            CombatActions.SelectTarget(target);
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new string[2];
            string target = actionData.Data.Value<string>("target");
            if (!names.ContainsValue(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
            string ability = actionData.Data.Value<string>("ability");
            if (!offense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
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
                string name = $"{enemy.Value.GetEnemyInfo().m_EnemyCombat.GetEnemyDisplay()}";
                // Timberwolf 1
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
        protected override string Description => "try to run away from combat. only the character this is used with will exit combat. this should be used for emergencies.";
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
        protected override string Description => "equip a different weapon. this will also end your turn without attacking.";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            return null;
            // JsonSchema schema = new()
            // {
            //     Type = JsonSchemaType.Object,
            //     Required = ["weapon"],
            //     Properties = new()
            //     {
            //         ["weapon"] = new() { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 16 }
            //     }
            // };
            // return schema;
        }

        protected override void Execute(string parsedData)
        {
            // Plugin.Logger.LogWarning("execute change weapon action: " + parsedData);
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


}