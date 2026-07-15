using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MovementAction(Dictionary<string, HexLand> _hexPositions) : NeuroAction<HexLand>
    {
        public static ActionWindow CreateAction(CharacterOverworld _instance, string ctx, List<INeuroAction> actions)
        {
            ActionWindow window = ActionWindow.Create(_instance.gameObject);
            if (actions.Count == 0) Plugin.Logger.LogError("no movement actions to register");
            window.SetContext(ctx);
            foreach (INeuroAction action in actions) window.AddAction(action);
            window.SetForce(0, "choose an action for this movement turn", "you have rolled for movement", true);
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
                OverworldMovement.CreateActionWindow();
                return;
            }
            OverworldMovement.ReverseCheckHoverPath(Movement.Instance, parsedData);
            if (!OverworldMovement.isTracking || ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogWarning("tried to execute move action while character is not in tracking state");
                Context.Send($"an issue occurred with the {Name} action", true);
                OverworldMovement.CreateActionWindow();
                return;
            }
            OverworldMovement.ReverseCheckClickPath(Movement.Instance, parsedData, false, false, false);
            Context.Send($"moving to {parsedData}");
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
        protected override string Description => "end the current turn early and recover HP from the remaining movement points";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            if (uiEndTurnButton.Instance.interactable) uiEndTurnButton.Instance.onClick.Invoke();
            else
            {
                Context.Send("cannot end turn right now");
                OverworldMovement.CreateActionWindow();
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class GoToHexAction(Dictionary<string, QuestLogicBase> _questDict) : NeuroAction<string>
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
            if (!_questDict.TryGetValue(parsedData, out QuestLogicBase quest))
            {
                Plugin.Logger.LogError("quest not found");
                Context.Send($"an issue occurred with the {Name} action, try another one", true);
                OverworldMovement.CreateActionWindow();
                return;
            }
            // hover destination to generate path list
            HexLand dest = quest.GetHexLandDestination();
            OverworldMovement.ReverseCheckHoverPath(Movement.Instance, dest);
            if (!OverworldMovement.isTracking || ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogWarning("tried to execute move action while character is not in tracking state");
                Context.Send($"an issue occurred with the {Name} action, try another one", true);
                return;
            }
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            // the generated move path
            dest = Movement.Instance.m_HexListPartial.Last();
            bool failed = true;
            for (int i = Movement.Instance.m_HexListPartial.Count-1; i >= 0; i--)
            {
                if (OverworldMovement.CanTravel(dest, cow))
                {
                    dest = Movement.Instance.m_HexListPartial[i];
                    failed = false;
                    break;
                }
                Plugin.Logger.LogWarning("cant auto travel to last hex");
            }
            if (failed)
            {
                Plugin.Logger.LogError("failed to auto travel to last hex");
                Context.Send($"an issue occurred with the {Name} action, try another one", true);
                return;
            }
            OverworldMovement.ReverseCheckClickPath(Movement.Instance, dest, false, false, false);
            Context.Send($"moving to {parsedData}", true);
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
                Context.Send("this character is not on a tile with something to interact with");
                OverworldMovement.CreateActionWindow();
                return;
            }
            OverworldMovement.ReverseCheckHoverPath(Movement.Instance, hex);
            if (!OverworldMovement.isTracking || ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogWarning("tried to execute move action while character is not in tracking state");
                Context.Send($"an issue occurred with the {Name} action", true);
                return;
            }
            OverworldMovement.ReverseCheckClickPath(Movement.Instance, hex, false, false, false);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}