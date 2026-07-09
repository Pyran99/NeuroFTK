using System;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
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
            window.SetForce(2, "choose a position that represents the tile you want to move to", "awaiting movement action", true);
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
            //"tile": "(168.8, 0.0, 37.5)"
            string data = actionData.Data.Value<string>("tile");
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("tile"));
            if (!hexPositions.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("tile"));
            parsedData = hexPositions[data];
            return ExecutionResult.Success();
        }

        /// <summary>
        /// display as [(position x,z) (name/realm)(quest name)other info]
        /// </summary>
        static string GetContext(List<HexLand> tiles)
        {
            // [(155.8, 20.0): (The Guardian Forest)(). Woodsmoke]
            string context = "[all tiles in range] (displayed as [(position x,z) (name/realm)(quest name)other info]) ";
            string name;
            string questName = "";
            string hasDeadPlayers = "";
            string poi = "";
            Vector3 itemPos;
            Vector2 pos;
            // FTK_realm.ID realm;
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            // float distance = 0f; // testing things
            foreach (HexLand hex in tiles)
            {
                poi = "";
                hasDeadPlayers = "";
                questName = "";
                // realm = hex.GetRealm();
                // GuardianForest | GoldenPlains
                // Plugin.Logger.LogWarning("realm: " + realm);
                // distance = (float)Math.Round(HexLand.Distance(cow.m_HexLand, hex), 2);
                // Plugin.Logger.LogWarning("dist: " + distance);
                name = hex.GetLocationDisplayValue(cow);
                // name = item.ToString().Replace(" (HexLand)", "");
                itemPos = hex.GetPosition();
                pos = new Vector2(itemPos.x, itemPos.z);
                if (TileHasQuestObjective(hex))
                {
                    Plugin.Logger.LogWarning(hex.GetPOI());
                    Plugin.Logger.LogWarning(hex.GetPOI()?.GetEncounterQuest());
                    Plugin.Logger.LogWarning(hex.GetPOI()?.GetEncounterQuest()?.GetLocalizedOneLineDesc());
                    QuestLogicBase quest = hex.GetPOI().GetEncounterQuest();
                    // quest.GetLocalizedOneLineDesc();
                    // quest.GetCurrentDestinationLocation();
                    questName = quest.GetQuestDef()?.m_DisplayName;
                    // questName = "has quest";
                }
                if (hex.GetDeadPlayerCount() > 0)
                {
                    hasDeadPlayers = "has dead character to revive";
                }
                if (hex.GetPOI() != null)
                {
                    poi = hex.GetPOI().GetPOIDisplayValue();
                }
                context += $"[{pos} ({name})({questName}){hasDeadPlayers + ". "}{poi}]\n";
                hexPositions.Add(pos.ToString(), hex);
            }
            return context;
        }

        static bool TileHasQuestObjective(HexLand hex)
        {
            bool result = hex.GetPOI()?.HasEncounterQuest() ?? false;
            Plugin.Logger.LogWarning(result);
            return result;
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