using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MovementAction(Dictionary<string, HexLand> _hexPositions, CharacterOverworld cow) : NeuroAction<HexLand>
    {
        public static ActionWindow CreateAction(CharacterOverworld _cow, string ctx, Dictionary<string, HexLand> hexPositions, Dictionary<string, QuestLogicBase> questDict)
        {
            ActionWindow window = ActionWindow.Create(_cow.gameObject);
            window.AddAction(new MovementAction(hexPositions, _cow));
            if (!GlobalConfig.debug_mode) window.AddAction(new EndTurnAction());
            if (questDict != null & questDict.Count > 0)
            {
                window.AddAction(new GoToQuestAction(new(questDict)));
            }
            if (_cow.GetHexLand()?.HasPOI() ?? false)
            {
                window.AddAction(new InteractWithCurrentHex());
            }
            window.SetContext(ctx);
            window.SetForce(0, "choose an action for this movement turn. you should try to keep your team near eachother to make fights easier.", "", true);
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
                OverworldFlow.CreateActionWindow(cow);
                return;
            }
            Context.Send($"moving to {OverworldFlow.GetContextForHex(cow, parsedData)}");
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
                OverworldFlow.CreateActionWindow(GameLogic.Instance.GetCurrentCOW());
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class GoToQuestAction(Dictionary<string, QuestLogicBase> _questDict) : NeuroAction<string>
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
                    ["destination"] = QJS.Enum(_questDict.Select(kvp => kvp.Key)),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            if (!_questDict.TryGetValue(parsedData, out QuestLogicBase quest))
            {
                Plugin.Logger.LogError("quest not found");
                Context.Send($"an issue occurred with the {Name} action, try another one", true);
                OverworldFlow.CreateActionWindow(cow);
                return;
            }
            HexLand dest = quest.GetHexLandDestination();
            cow.StartCoroutine(OverworldFlow.MoveToHexCoroutine(cow, dest, true));
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string data = actionData.Data.Value<string>("destination");
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("destination"));
            if (!_questDict.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("destination"));
            parsedData = data;
            return ExecutionResult.Success();
        }
    }

    public class InteractWithCurrentHex : NeuroAction
    {
        public override string Name => "interact_with_this_tile";
        protected override string Description => "interact with the point of interest on the tile the current character is at";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            HexLand hex = cow.GetHexLand();
            if (!hex.HasPOI())
            {
                Context.Send("this character is not on a tile with something to interact with", true);
                OverworldFlow.CreateActionWindow(cow);
                // MovementAction.CreateAction(cow, "", OverworldFlow.hexPositions, OverworldFlow.questDict);
                return;
            }
            cow.StartCoroutine(OverworldFlow.MoveToHexCoroutine(cow, hex, false, true));
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}