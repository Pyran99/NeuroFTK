using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MovementAction() : NeuroAction<HexLand>
    {
        public static ActionWindow RegisterAction(GameObject owner, List<HexLand> _tiles)
        {
            hexPositions.Clear();
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new MovementAction());
            window.AddAction(new EndTurnAction());
            // window.SetForce(5, "choose a position that represents the tile you want to move to", "awaiting movement action", true);
            window.SetContext(GetContext(_tiles));
            window.Register();
            return window;
        }

        public static Dictionary<string, HexLand> hexPositions = [];

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
                    ["tile"] = QJS.Enum(hexPositions.Select(x => x.Key).ToList()),
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
                return;
            }
            Plugin.Logger.LogMessage($"executing movement action to {parsedData}");
            OverworldMovement.tiles.Clear();
            Movement.Instance.m_CursorHex = parsedData; // needs to be set
            if (!OverworldMovement.isTracking || ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogWarning("tried to execute move action while character is not in tracking state");
                Context.Send($"an issue occurred with the {Name} action", true);
                return;
            }
            OverworldMovement.ReverseCheckClickPath(Movement.Instance, parsedData, false, false, false);
        }


        protected override ExecutionResult Validate(ActionJData actionData, out HexLand parsedData)
        {
            parsedData = null;
            //"(x, y, z)"
            Plugin.Logger.LogMessage(actionData.Data);
            string data = actionData.Data.Value<string>("tile");
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("tile"));
            if (!hexPositions.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("tile"));
            parsedData = hexPositions[data];
            return ExecutionResult.Success();
        }

        /// <summary>
        /// display as ({name}(quest name): {position})
        /// </summary>
        static string GetContext(List<HexLand> tiles)
        {
            string context = "[these are the tiles in range (displayed as [(position(as x,y,z)): (name)(quest name)])]: ";
            string name;
            string questName = "";
            Vector3 pos;
            foreach (HexLand item in tiles)
            {
                name = item.ToString().Replace(" (HexLand)", "");
                pos = item.GetPosition();
                if (TileHasQuestObjective(item))
                {
                    questName = "has quest";
                }
                context += $"[({pos}): ({name})({questName})]";
                hexPositions.Add(pos.ToString(), item);
            }
            return context;
        }

        //TODO
        static bool TileHasQuestObjective(HexLand hex)
        {

            return false;
        }
    }

    public class EndTurnAction : NeuroAction
    {
        public override string Name => "end_turn";
        protected override string Description => "end the current turn early and recover HP from the remaining movement points.";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            if (uiEndTurnButton.Instance.interactable) uiEndTurnButton.Instance.onClick.Invoke();
            else
            {
                Context.Send("cannot end turn right now");
                MovementAction.RegisterAction(GameLogic.Instance.GetCurrentCOW().gameObject, OverworldMovement.tiles);
            }
            
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}