using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MovementAction : NeuroAction<HexLand>
    {
        // // copy from CombatActions
        // public static ActionWindow CreateAction(uiBattleStanceButtons _instance, string ctx, List<INeuroAction> actions)
        // {
            
        // }


        public static ActionWindow RegisterAction(GameObject owner, List<HexLand> _tiles)
        {
            hexPositions.Clear();
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new MovementAction());
            if (!GlobalConfig.debug_mode)
            {
                window.AddAction(new EndTurnAction());
            }
            if (GameLogic.Instance.GetQuestTable().Count > 0)
            {
                window.AddAction(new GoToHexAction());
            }
            window.SetForce(0, "choose a position that represents the tile you want to move to", "awaiting movement action", true);
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
            // OverworldMovement.tiles.Clear();
            Movement.Instance.m_CursorHex = parsedData; // needs to be set
            Movement.Instance.UpdateCursorHex();
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
            string context = "[all tiles in range] (displayed as [(position x,z) (name/realm)(quest)other info]) ";
            string name;
            string questName = "";
            string hasDeadPlayers = "";
            string characters = ""; //TODO
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
                if (TileHasQuestObjective(hex, out QuestLogicBase _quest))
                {
                    MiniHexInfo hexPOI = hex.GetPOI();
                    Plugin.Logger.LogWarning(hexPOI);
                    if (_quest != null)
                    {
                        questName = _quest.GetQuestDef()?.m_DisplayName;
                        if (questName.IsNullOrEmpty()) questName = "is quest location";
                        Plugin.Logger.LogWarning("tile quest obj: " + StringReplace.RemoveStyling(_quest.GetLocalizedOneLineDesc()));
                        // Kill the <color=#FBB060>Chaos Leader</color> in <color=#FBB060>The Guardian Forest</color>
                        // quest.GetCurrentDestinationLocation();
                    }
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

        static bool TileHasQuestObjective(HexLand hex, out QuestLogicBase quest)
        {
            MiniHexInfo poi = hex.GetPOI();
            quest = poi?.GetEncounterQuest();
            bool result = quest != null;
            if (!result)
            {
                if (poi?.GetFirstQuest() != null)
                {
                    quest = poi.GetFirstQuest();
                    result = true;
                }
            }
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
                if (OverworldMovement.tiles.Count > 0)
                {
                    MovementAction.RegisterAction(GameLogic.Instance.GetCurrentCOW().gameObject, OverworldMovement.tiles);
                }
            }
            
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class GoToHexAction : NeuroAction<string>
    {
        readonly Dictionary<string, QuestLogicBase> questDict = [];

        public override string Name => "go_to_quest";
        protected override string Description => "choose a quest location to go to";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            GetQuests(); // remove when done
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["destination"],
                Properties = new()
                {
                    ["destination"] = QJS.Enum(questDict.Select(kvp => kvp.Key)),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogWarning($"Execute GoTo {parsedData}");
            if (!questDict.TryGetValue(parsedData, out QuestLogicBase quest))
            {
                Plugin.Logger.LogError("quest not found");
                return;
            }
            HexLand dest = quest.GetHexLandDestination();
            // OverworldMovement.tiles.Clear();
            // Movement.Instance.m_CursorHex = dest; // needs to be set
            // Movement.Instance.UpdateCursorHex();
            OverworldMovement.ReverseCheckHoverPath(Movement.Instance, dest);
            if (!OverworldMovement.isTracking || ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogWarning("tried to execute move action while character is not in tracking state");
                Context.Send($"an issue occurred with the {Name} action", true);
                return;
            }
            //for neuro auto-walk => select desired tile, then choose last from movement list
            HexLand last = Movement.Instance.m_HexListPartial.Last();
            dest = last;
            if (!OverworldMovement.CanTravel(dest, GameLogic.Instance.GetCurrentCOW()))
            {
                Plugin.Logger.LogWarning("cant auto travel to last hex");
                return;
            }
            OverworldMovement.ReverseCheckClickPath(Movement.Instance, dest, false, false, false);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogWarning("validate GoTo");
            parsedData = "";
            string data = actionData.Data.Value<string>("destination");
            if (data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("destination"));
            if (!questDict.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("destination"));
            parsedData = data;
            return ExecutionResult.Success();
        }

        //TODO remove when done
        void GetQuests()
        {
            questDict.Clear();
            List<uiQuestItem> storyQuests = [];
            List<uiQuestItem> sideQuests = [];
            uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren(false, storyQuests);
            uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren(false, sideQuests);
            foreach (uiQuestItem q in storyQuests)
            {
                OverworldMovement.AddValidQuests(q);
            }
            foreach (uiQuestItem q in sideQuests)
            {
                OverworldMovement.AddValidQuests(q);
            }
            return;
        }

    }
}