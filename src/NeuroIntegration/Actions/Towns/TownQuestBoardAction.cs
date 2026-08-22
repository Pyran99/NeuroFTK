using System.Collections.Generic;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK
{
    public class TownQuestBoardAction(Dictionary<string, QuestListItem> _items) : NeuroAction<string>
    {
        public static ActionWindow CreateWindow(Dictionary<string, QuestListItem> _items)
        {
            ActionWindow window = ActionWindow.Create(uiGetQuestMenu.Instance.m_ListRoot.gameObject);
            window.AddAction(new TownQuestBoardAction(_items));
            window.SetForce(0, "choose a side quest for additional rewards", "you are at a towns quest board", true);
            window.Register();
            return window;
        }

        public override string Name => "choose_quest";
        protected override string Description => "choose a side quest to accept from this town. only 1 quest can be accepted from each town at a time";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["quest"],
                Properties = new()
                {
                    ["quest"] = QJS.Enum(_items.Keys)
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogWarning("execute " + parsedData);
            TownQuestBoard.NeuroDecision(_items[parsedData]);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            if (actionData.Data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("quest"));
            parsedData = actionData.Data?.Value<string>("quest") ?? "null";
            if (!_items.ContainsKey(parsedData)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("quest"));
            return ExecutionResult.Success();
        }
    }
}