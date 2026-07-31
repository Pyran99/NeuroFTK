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
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MovementAction(Dictionary<string, HexLand> _hexPositions, CharacterOverworld cow) : NeuroAction<HexLand>
    {
        public static ActionWindow CreateWindow(CharacterOverworld _cow, string ctx, Dictionary<string, HexLand> hexPositions, Dictionary<string, QuestLogicBase> questDict)
        {
            ActionWindow window = ActionWindow.Create(_cow.gameObject);
            window.AddAction(new MovementAction(hexPositions, _cow));
            if (!OverworldFlow.isSneakMovement)
            {
                if (!GlobalConfig.IsDebugMode()) window.AddAction(new EndTurnAction());
                if (questDict != null && questDict.Count > 0)
                {
                    if (questDict.Count != 1 || questDict.First().Value.GetHexLandDestination()?.GetPosition() != _cow.GetHexLand().GetPosition())
                    {
                        window.AddAction(new GoToQuestAction(new(questDict), _cow));
                    }
                }
                HexLand hex = _cow.GetHexLand();
                if (hex?.HasPOI() ?? false)
                {
                    MiniHexInfo poi = hex.GetPOI();
                    Plugin.Logger.LogWarning("poi = " + poi.GetIDString());
                    if (!HexData.IsPoiComplete(poi) || poi.m_MiniHexType == MiniHexInfo.MiniHexType.Town) window.AddAction(new InteractWithCurrentHex(_cow));
                }
            }
            window.SetContext(ctx);
            window.SetForce(0, "choose an action for this movement turn. you should try to keep your team near eachother to make fights easier.", "you are moving your characters around the overworld", true);
            window.Register();
            return window;
        }

        public static ActionWindow CreateTurnBeginWindow(bool registerBelt = true)
        {
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
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
                registerActions.Add(new UseBeltItemAction(items, cow, true));
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
                OverworldFlow.CreateActionWindow(cow);
                return;
            }
            Context.Send($"moving to {HexData.GetContextForHex(cow, parsedData)}");
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

    public class GoToQuestAction(Dictionary<string, QuestLogicBase> _questDict, CharacterOverworld _cow) : NeuroAction<string>
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
                    ["destination"] = QJS.Enum(GetInRangeQuests()),
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
                Context.Send($"an issue occurred with the {Name} action, try another one (if your choice was 'none' then you should choose a different action)", true);
                QuickTimerCallback timer = new(() => OverworldFlow.CreateActionWindow(cow), cow.gameObject, 0.5f);
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
            if (data == "none") return ExecutionResult.Success();
            if (!_questDict.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("destination"));
            parsedData = data;
            return ExecutionResult.Success();
        }

        List<string> GetInRangeQuests()
        {
            List<string> result = [];
            List<Vector3> positions = OverworldFlow.GetQuestPositions();
            foreach (KeyValuePair<string, QuestLogicBase> kvp in _questDict)
            {
                Vector3 dest = kvp.Value.GetHexLandDestination()?.GetPosition() ?? Vector3.positiveInfinity;
                if (dest == _cow.GetHexLand().GetPosition()) continue;
                if (positions.Contains(dest))
                {
                    if ((dest - _cow.GetHexLand().GetPosition()).magnitude < 2.8866f * 15f)
                    {
                        result.Add(kvp.Key);
                    }
                }
            }
            if (result.Count == 0)
            {
                Plugin.Logger.LogError("there were no valid quests for the action");
                result.Add("none");
            }
            return result;
        }
    }

    public class InteractWithCurrentHex(CharacterOverworld cow) : NeuroAction
    {
        public override string Name => "interact_with_this_tile";
        protected override string Description => "interact with the point of interest on the tile the current character is at";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            HexLand hex = cow.GetHexLand();
            if (!hex.HasPOI())
            {
                Context.Send("this character is not on a tile with something to interact with" + NeuroSdkStrings.ModFaultSuffix, true);
                QuickTimerCallback timer = new(() => OverworldFlow.CreateActionWindow(cow), cow.gameObject, 0.5f);
                return;
            }
            MiniHexInfo poi = hex.GetPOI();
            if (!IsInteractable(poi))
            {
                Plugin.Logger.LogError($"{poi} was not interactable");
                return;
            }
            cow.StartCoroutine(OverworldFlow.MoveToHexCoroutine(cow, hex, false, true));
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }

        bool IsInteractable(MiniHexInfo poi)
        {
            Plugin.Logger.LogMessage("poi type = " + poi.m_MiniHexType);
            if (poi.m_Deactivated)
            {
                Context.Send("this tile has been deactivated");
                QuickTimerCallback timer = new(() => OverworldFlow.CreateActionWindow(cow), cow.gameObject, 0.5f);
                return false;
            }
            bool interactable = true;
            if (poi is MiniHexAlluringPool)
            {
                if ((poi as MiniHexAlluringPool).GetAlluringPoolOptions().Count == 0)
                {
                    Context.Send("you need to find other alluring pools to activate the teleport system");
                    interactable = false;
                }
            }
            if (poi is MiniEncounter)
            {
                MiniEncounter encounter = poi as MiniEncounter;
                Plugin.Logger.LogMessage("poi encounter type = " + (poi as MiniEncounter).m_Type);
                if (encounter.m_HasBeenConsumed || encounter.m_CantUseThisTurn) interactable = false;
                if (encounter.m_Type == FTK_miniEncounter.ID.kvHome)
                {
                    Context.Send($"{CharacterData.GetCharacterName(cow)} does not have the required quest item for this hex");
                    interactable = false;
                }
            }
            if (poi is MiniHexDungeon)
            {
                //VERIFY failed remake actions after interact with dungeon while party not ready
                MiniHexDungeon dungeon = poi as MiniHexDungeon;
                if (!dungeon.IsDungeonCleared())
                {
                    List<FTKPlayerID> readyPlayers = dungeon.GetLoadPartyPlayers(cow, GameFlow.CombatType.Fight);
                    int num = 0;
                    foreach (CharacterOverworld _cow in FTKHub.Instance.m_CharacterOverworlds)
                    {
                        if (!readyPlayers.Contains(_cow.m_FTKPlayerID))
                        {
                            if (!GameFlow.Instance.IsPermaDeath || !_cow.m_WaitForRespawn)
                            {
                                num++;
                            }
                        }
                    }
                    if (num != 0)
                    {
                        if (dungeon.m_ID != FTK_dungeonEncounter.ID.Harazuel)
                        {
                            Context.Send("your entire party needs to be alive and within range to enter the dungeon");
                            interactable = false;
                        }
                    }
                }
            }
            if (!interactable)
            {
                QuickTimerCallback timer = new(() => OverworldFlow.CreateActionWindow(cow), cow.gameObject, 0.5f);
            }
            return interactable;
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