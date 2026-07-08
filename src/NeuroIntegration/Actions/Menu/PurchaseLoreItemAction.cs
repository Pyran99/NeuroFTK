using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.HarmonyPatches;
using System.Text;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItemAction(uiLoreStore _store, Dictionary<string, string> _schemaData) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(uiLoreStore instance, Dictionary<string, string> _schemaData)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            PurchaseLoreItemAction action = new(instance, _schemaData);
            window.AddAction(action);
            CancelAction cancel = new(window, "return to main menu");
            cancel.OnCancelled += LoreStoreUnlocks.OnActionCancelled;
            window.AddAction(cancel);
            StringBuilder sb = new();
            foreach (string key in _schemaData.Keys)
            {
                sb.AppendLine($"[{key}] {StringReplace.ReplaceNewLine(_schemaData[key])}");
            }
            window.SetContext($"(Items and their descriptions you can afford) {sb}");
            window.SetForce(5, "purchase lore items from a category or cancel the action and go back to the main menu if you dont want to purchase anything right now", "You are in the lore store for game unlocks");
            window.Register();
            return window;
        }


        public uiLoreStore uiLoreStore = _store;
        public Dictionary<string, string> schemaData = new(_schemaData);

        public override string Name => "purchase_lore_item";
        protected override string Description => "purchase an item from the store. these unlock various things that can appear in future runs.";
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            List<string> data = [.. schemaData.Select(l => l.Key)];
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["item"],
                Properties = new()
                {
                    ["item"] = QJS.Enum(data),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            if (LoreStoreUnlocks.isPurchasing)
            {
                Plugin.Logger.LogWarning("duplicate store purchase");
                return;
            }
            LoreStoreUnlocks.isPurchasing = true;
            uiLoreStore.StartCoroutine(LoreStoreUnlocks.DoPurchase(LoreStoreUnlocks.availableLoreData[parsedData]["card"] as uiLoreCard, parsedData));
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data.Value<string>("item");
            if (result == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("item"));
            if (!schemaData.ContainsKey(result))
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            }
            parsedData = result;
            return ExecutionResult.Success();
        }
    }
}


