using System.Collections.Generic;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK
{
    public class MarketPurchaseAction(Dictionary<string, uiItemIcon> _items) : NeuroAction<object[]>
    {

        public override string Name => "market_purchase";
        protected override string Description => "purchase an item from the market. if the item is equipment, you can choose to equip it immediately, replacing what is currently equipped in the same slot.";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["item", "equip"],
                Properties = new()
                {
                    ["item"] = QJS.Enum(_items.Keys),
                    ["equip"] = QJS.Type(JsonSchemaType.Boolean)
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            _items.TryGetValue((string)parsedData[0], out uiItemIcon result);
            if (result == null)
            {
                Plugin.Logger.LogError("issue with market purchase");
                Context.Send("issue with market purchase" + NeuroSdkStrings.ModFaultSuffix);
                TownMarket.CloseMenu();
                return;
            }
            TownMarket.NeuroDecision(result, (bool)parsedData[1]);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[2];
            Plugin.Logger.LogWarning("data: " + actionData.Data.ToString());
//   data: {
//   "item": "Panax",
//   "equip": false
// }
            if (actionData.Data == null) return ExecutionResult.Failure("invalid data");
            string item = actionData.Data.Value<string>("item") ?? "null";
            bool equip = actionData.Data.Value<bool>("equip");
            if (!_items.ContainsKey(item)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            if (equip.GetType() != typeof(bool)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("equip"));
            parsedData[0] = item;
            parsedData[1] = equip;
            return ExecutionResult.Success();
        }
    }
}