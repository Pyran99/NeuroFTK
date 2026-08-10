using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MovementAction(Dictionary<string, HexLand> _hexPositions, CharacterOverworld cow) : NeuroAction<HexLand>
    {
        public static ActionWindow CreateWindow(CharacterOverworld _cow, string ctx, Dictionary<string, HexLand> hexPositions, Dictionary<string, QuestLogicBase> questDict, List<string> validQuests, IEnumerable<CharacterOverworld> validCows, bool isInteractable = false)
        {
            ActionWindow window = ActionWindow.Create(_cow.gameObject);
            window.AddAction(new MovementAction(hexPositions, _cow));
            if (!OverworldFlow.isSneakMovement)
            {
                if (!GlobalConfig.IsDebugMode()) window.AddAction(new EndTurnAction());
                if (validQuests.Count > 0)
                {
                    window.AddAction(new GoToQuestAction(questDict, validQuests));
                }
                if (validCows.Count() > 0)
                {
                    window.AddAction(new GoToCharacterAction(validCows.ToDictionary(CharacterData.GetCharacterName, x => x)));
                }
                if (isInteractable && !_cow.IsInBoat()) window.AddAction(new InteractWithCurrentHex(_cow));
            }
            window.SetContext(ctx);
            window.SetForce(0, "choose an action for this movement turn. you should try to keep your team near eachother to make fights easier.", "you are moving your characters around the overworld", true);
            window.Register();
            return window;
        }

        public static ActionWindow CreateTurnBeginWindow(bool registerBelt = true)
        {
            CharacterOverworld cow = CharacterData.GetNeuroCow();
            ActionWindow window = ActionWindow.Create(cow.gameObject);
            List<INeuroAction> registerActions = [];
            registerActions.Add(new BeginMovementAction());

            List<FTK_itembase.ID> beltItems = ItemData.GetUsableBeltItems(cow);
            Dictionary<string, FTK_itembase.ID> items = [];
            StringBuilder beltCtx = new();
            beltCtx.Append("[usable belt items] ");
            if (registerBelt)
            {
                foreach (FTK_itembase.ID item in beltItems)
                {
                    items.Add(ItemData.GetItemName(item), item);
                    beltCtx.AppendLine($"({ItemData.GetItemName(item)}) {ItemData.GetItemDescription(item, true, cow)}");
                }
            }
            if (items.Count > 0)
            {
                registerActions.Add(new UseBeltItemAction(items, cow));
                Context.Send(beltCtx.ToString());
            }
            string query = $"your turn for {CharacterData.GetCharacterName(cow)} has started. use items or begin your movement choices";
            foreach (INeuroAction action in registerActions) window.AddAction(action);
            window.SetContext(BeginTurns.CtxOverworldTurnBeginStats(cow));
            window.SetForce(5, query, "", true);
            window.Register();
            return window;
        }

        public override string Name => "overworld_movement";
        protected override string Description => "choose a tile position to move the current character to";
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["tile"],
                Properties = new()
                {
                    ["tile"] = QJS.Enum(_hexPositions.Select(x => x.Key).ToList()),
                }
            };
            return schema;
        }

        protected override void Execute(HexLand parsedData)
        {
            // FTKHex.Instance.GetHexLand(int, int);
            if (parsedData == null)
            {
                Plugin.Logger.LogError($"did not find {parsedData} in tiles");
                Context.Send($"an issue occurred with the {Name} action", true);
                OverworldFlow.CreateMovementActions(cow);
                return;
            }
            cow.StartCoroutine(OverworldFlow.MoveToHexCoroutine(cow, parsedData));
        }


        protected override ExecutionResult Validate(ActionJData actionData, out HexLand parsedData)
        {
            parsedData = null;
            //"tile": "(168.8, 37.5)"
            string data = actionData.Data.Value<string>("tile");
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("tile"));
            if (!_hexPositions.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("tile"));
            parsedData = _hexPositions[data];
            return ExecutionResult.Success();
        }
    }

    public class EndTurnAction : NeuroAction
    {
        public override string Name => "end_turn";
        protected override string Description => "end your turn early and recover HP from the remaining movement points";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            if (uiEndTurnButton.Instance.interactable) uiEndTurnButton.Instance.onClick.Invoke();
            else
            {
                Context.Send("cannot end turn right now");
                OverworldFlow.CreateMovementActions(CharacterData.GetNeuroCow());
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class GoToQuestAction(Dictionary<string, QuestLogicBase> _questDict, List<string> validQuests) : NeuroAction<string>
    {
        public override string Name => "go_to_quest";
        protected override string Description => "choose a quest location to travel to. if the location is out of range you will move to the furthest tile along the path";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["destination"],
                Properties = new()
                {
                    ["destination"] = QJS.Enum(validQuests),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            CharacterOverworld cow = CharacterData.GetNeuroCow();
            OverworldFlow.NeuroTryGoToQuest(cow, _questDict.TryGetValue(parsedData, out QuestLogicBase quest) ? quest : null);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string data = actionData.Data.Value<string>("destination");
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("destination"));
            if (data == "none") return ExecutionResult.Success();
            if (!_questDict.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("destination"));
            parsedData = data;
            return ExecutionResult.Success();
        }
    }

    public class GoToCharacterAction(Dictionary<string, CharacterOverworld> _characterDict) : NeuroAction<string>
    {
        public override string Name => "go_to_character";
        protected override string Description => "choose a character to travel to. if the location is out of range you will move to the furthest tile along the path. this action is useful for keeping your team together";
        protected override JsonSchema Schema => GetSchema();
        readonly string prop = "character";

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = [prop],
                Properties = new()
                {
                    [prop] = QJS.Enum(_characterDict.Select(x => x.Key).ToList()),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            _characterDict.TryGetValue(parsedData, out CharacterOverworld character);
            OverworldFlow.NeuroTryGoToCharacter(character);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string data = actionData.Data.Value<string>(prop);
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(prop));
            if (!_characterDict.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format(prop));
            parsedData = data;
            return ExecutionResult.Success();
        }
    }

    public class InteractWithCurrentHex(CharacterOverworld cow) : NeuroAction
    {
        public override string Name => "interact_with_this_hex";
        protected override string Description => "interact with the point of interest on the hex the current character is at";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            OverworldFlow.NeuroTryInteractWithHex(cow);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class BeginMovementAction : NeuroAction
    {
        public override string Name => "begin_movement";
        protected override string Description => "begins your movement choice";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            QuickTimerCallback timer = new(OverworldFlow.BeginMovementTurn, Movement.Instance.gameObject);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class ChangeEquipment : NeuroAction
    {
        public override string Name => "change_equipment";
        protected override string Description => "";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}