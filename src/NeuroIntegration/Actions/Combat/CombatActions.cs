using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{

    public class CombatActions
    {
        static uiBattleStanceButtons instance;

        public static ActionWindow RegisterCombatActions(uiBattleStanceButtons _instance, string ctx, string state, List<INeuroAction> actions)
        {
            instance = _instance;
            if (actions.Count == 0)
            {
                Plugin.Logger.LogError("no combat actions to register");
                Context.Send("something went very wrong with the combat actions, let vedal know there is a problem");
                actions.Add(new CombatFleeAction(_instance.m_FleeButton));
            }
            ActionWindow window = ActionWindow.Create(_instance.gameObject);
            window.SetContext(ctx);
            foreach (INeuroAction action in actions)
            {
                window.AddAction(action);
            }
            window.SetForce(0, $"it is your turn with {CharacterData.GetCharacterName(instance.CombatCow)}, choose an action", state, true);
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
            CharacterOverworld cow = CharacterData.GetActiveCow();
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["target", "ability"],
                Properties = new()
                {
                    ["ability"] = QJS.Enum(offense.Keys),
                    ["target"] = QJS.Enum(GetListOfEnemies().Values),
                    ["focus"] = CharacterData.QuickFocusSchema(cow)
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            // Plugin.Logger.LogMessage("execute attack: " + parsedData[0] + ", " + parsedData[1] + ", " + parsedData[2]);
            uiPlayerMainHud.CloseItemCard();
            offense.TryGetValue((string)parsedData[1], out uiBattleButton btn);
            FTKPlayerID target = names.First(v => v.Value == (string)parsedData[0]).Key;
            if (target == null)
            {
                Plugin.Logger.LogError("target is null " + parsedData[0]);
                return;
            }
            btn.OnPointerEnter(null); // sets combat profile
            CombatActions.SelectTarget(target);
            CharacterStats stats = CharacterData.GetActiveCow().m_CharacterStats;
            if (CharacterData.CanFocusAction(stats, btn.m_Owner.m_CombatActionProfile.m_Slots, (int)parsedData[2]) && !btn.m_Owner.m_CombatActionProfile.m_NoFocus)
            {
                btn.StartCoroutine(SelectButton.UseFocus((int)parsedData[2], btn, stats));
            }
            else SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[3];
            // Plugin.Logger.LogWarning(actionData.Data);
            string target = actionData.Data?.Value<string>("target");
            if (target.IsNullOrEmpty() || !names.ContainsValue(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
            string ability = actionData.Data?.Value<string>("ability");
            if (ability.IsNullOrEmpty() || !offense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
            int focus = actionData.Data?.Value<int>("focus") ?? 0;
            parsedData[0] = target;
            parsedData[1] = ability;
            parsedData[2] = focus;
            return ExecutionResult.Success();
        }

        Dictionary<FTKPlayerID, string> GetListOfEnemies()
        {
            names = [];
            int count = 0;
            Dictionary<FTKPlayerID, EnemyDummy> enemies = new(EncounterSession.Instance.m_EnemyDummies);
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
    /// actions to target friendly units
    /// </summary>
    public class CombatFriendlyAction(Dictionary<string, uiBattleButton> _defense) : NeuroAction<object[]>
    {
        readonly Dictionary<string, uiBattleButton> defense = new(_defense);

        public override string Name => "buff_ally";
        protected override string Description => "heal/buff an ally or self. if the ability can be used on an ally you will be able to choose the target after";
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
                    ["focus"] = CharacterData.QuickFocusSchema(CharacterData.GetActiveCow())
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            Plugin.Logger.LogMessage("execute attack: " + string.Concat(parsedData[0], parsedData[1]));
            defense.TryGetValue((string)parsedData[0], out uiBattleButton btn);
            ChooseRewardMenu.teamState = $"you can apply your action {parsedData[0]} to \n" + BeginTurns.GetSimplifiedTeamState();
            CharacterOverworld cow = CharacterData.GetActiveCow();
            btn.OnPointerEnter(null);
            if (CharacterData.CanFocusAction(cow.m_CharacterStats, btn.m_Owner.m_CombatActionProfile.m_Slots, (int)parsedData[1]) && !btn.m_Owner.m_CombatActionProfile.m_NoFocus)
            {
                btn.StartCoroutine(SelectButton.UseFocus((int)parsedData[1], btn, cow.m_CharacterStats));
            }
            else SelectButton.StartCoroutine(btn, 0.5f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[2];
            string ability = actionData.Data?.Value<string>("ability");
            if (!defense.ContainsKey(ability)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("ability"));
            parsedData[0] = ability;
            parsedData[1] = actionData.Data?.Value<int>("focus") ?? 0;
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// try to flee combat
    /// </summary>
    public class CombatFleeAction(uiBattleButton btn): NeuroAction<int>
    {
        public override string Name => "flee_combat";
        protected override string Description => "try to run away from combat. only the character this is used with will exit combat. this should be used for emergencies.";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Properties = new()
            {
                ["focus"] = CharacterData.QuickFocusSchema(CharacterData.GetActiveCow())
            }
        };

        protected override void Execute(int parsedData)
        {
            btn.OnPointerEnter(null);
            CharacterStats stats = CharacterData.GetActiveCow().m_CharacterStats;
            if (CharacterData.CanFocusAction(stats, btn.m_Owner.m_CombatActionProfile.m_Slots, parsedData) && !btn.m_Owner.m_CombatActionProfile.m_NoFocus)
            {
                btn.StartCoroutine(SelectButton.UseFocus(parsedData, btn, stats));
            }
            else SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out int parsedData)
        {
            parsedData = actionData?.Data?.Value<int>("focus") ?? 0;
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// force enemies to attack this unit
    /// </summary>
    public class CombatTauntAction(uiBattleButton btn) : NeuroAction<int>
    {
        public override string Name => "taunt";
        protected override string Description => "try to force enemies to attack this unit";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Properties = new()
            {
                ["focus"] = CharacterData.QuickFocusSchema(CharacterData.GetActiveCow())
            }
        };

        protected override void Execute(int parsedData)
        {
            btn.OnPointerEnter(null);
            CharacterStats stats = CharacterData.GetActiveCow().m_CharacterStats;
            if (CharacterData.CanFocusAction(stats, btn.m_Owner.m_CombatActionProfile.m_Slots, parsedData) && !btn.m_Owner.m_CombatActionProfile.m_NoFocus)
            {
                btn.StartCoroutine(SelectButton.UseFocus(parsedData, btn, stats));
            }
            else SelectButton.StartCoroutine(btn, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out int parsedData)
        {
            parsedData = actionData?.Data?.Value<int>("focus") ?? 0;
            return ExecutionResult.Success();
        }
    }

    /// <summary>
    /// heal all party members
    /// </summary>
    public class CombatPartyHealAction(uiBattleButton btn): NeuroAction
    {
        public override string Name => "party_heal";
        protected override string Description => "uses godsbeard to heal all party members";
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
    /// change weapon
    /// </summary>
    public class CombatChangeWeaponAction(uiBattleButton btn): NeuroAction<string>
    {
        public override string Name => "change_weapon";
        protected override string Description => "equip a different weapon. this will also end your turn without attacking.";
        protected override JsonSchema Schema => null;

        private JsonSchema GetSchema()
        {
            return null;
        }

        protected override void Execute(string parsedData)
        {
            FTK_itembase itemBase;
            List<FTK_itembase.ID> items = [];
            CharacterOverworld cow = CharacterData.GetActiveCow();
            foreach (FTK_itembase.ID item in cow.m_PlayerInventory.m_ContainerBackpack.m_CountDictionary.Keys)
            {
                itemBase = FTK_itembase.GetItemBase(item);
                if (itemBase.m_ObjectType == FTK_itembase.ObjectType.weapon)
                {
                    if (items.Contains(item)) continue;
                    items.Add(item);
                }
            }
            StringBuilder sb = new($"## available weapons \n");
            foreach (FTK_itembase.ID item in items)
            {
                sb.AppendLine($"- {ItemData.GetItemName(item)}: {ItemData.GetItemDescription(item, cow, true, true)}");
            }
            sb.Append($"{CharacterData.GetCharacterName(cow)} prefers weapons that roll with {CharacterData.GetClassMainStat(cow.m_CharacterStats.m_CharacterClass)} stat");
            Context.Send(sb.ToString());
            SelectButton.StartCoroutine(btn);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData?.Data?.Value<string>("weapon") ?? "null";
            return ExecutionResult.Success();
        }
        
    }

#endregion


}