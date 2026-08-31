using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;
using Pyran.NeuroFTK.HarmonyPatches;
using WebSocketSharp;
using GridEditor;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItemAction(uiLoreStore _store, Dictionary<string, uiLoreCard> _availableCards) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(uiLoreStore instance, Dictionary<FTK_loreCategory, List<uiLoreCard>> categoryData)
        {
            Dictionary<string, uiLoreCard> availableCards = [];
            string cardsCtx = LoreItemData.GetCardListContext(categoryData, out availableCards);
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            PurchaseLoreItemAction action = new(instance, availableCards);
            window.AddAction(action);
            CancelAction cancel = new(window, "return to main menu");
            cancel.OnCancelled += LoreStoreUnlocks.OnActionCancelled;
            window.AddAction(cancel);
            // window.SetContext($"Items and their descriptions you can afford \n{cardsCtx}");
            window.SetForce(0, "purchase items from a category or cancel the action and go back to the main menu if you dont want to purchase anything right now. Character customization isnt implemented for you yet, so ", $"Items you can afford \n{cardsCtx}", true);
            window.Register();
            return window;
        }


        public uiLoreStore uiLoreStore = _store;

        public override string Name => "purchase_lore_item";
        protected override string Description => "purchase an item from the store. these unlock various things that can appear in future runs.";
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            List<string> data = [.. _availableCards.Select(l => l.Key)];
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
            uiLoreStore.StartCoroutine(LoreStoreUnlocks.DoPurchase(_availableCards[parsedData], parsedData));
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data?.Value<string>("item");
            if (result.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("item"));
            if (!_availableCards.ContainsKey(result))
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            }
            parsedData = result;
            return ExecutionResult.Success();
        }
    }
}


