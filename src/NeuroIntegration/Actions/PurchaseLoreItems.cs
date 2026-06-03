using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItems(uiLoreStore store) : NeuroAction<string>
    {
        public uiLoreStore uiLoreStore = store;

        public override string Name => "lore store purchase items";

        protected override string Description => "NYI";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.String,
            Required = ["test"],
            Properties = new Dictionary<string, JsonSchema>()
            {
                ["test"] = QJS.Enum(["1","2","3"])
            }
        };

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage("execute purchase lore items action");
            if (parsedData == "1")
            {
                Plugin.Logger.LogMessage("close store");
                uiLoreStore.OnClose();
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogMessage($"validate purchase lore items action: {actionData.Data}");
            parsedData = (string)actionData.Data;
            return ExecutionResult.Success();
        }
    }
}