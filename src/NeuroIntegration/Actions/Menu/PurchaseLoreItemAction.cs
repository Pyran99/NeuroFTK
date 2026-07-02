using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using StartGameFE;
using UnityEngine;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItemAction(uiLoreStore store, List<uiLoreCard> cards, Dictionary<string, string> _schemaData) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(uiLoreStore instance, List<uiLoreCard> cards, Dictionary<string, string> _schemaData)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            PurchaseLoreItemAction action = new(instance, cards, _schemaData);
            action.itemPurchased += LoreStoreUnlocks.OnItemPurchased;
            window.AddAction(action);
            CancelAction cancelAction = new(window, "return to main menu");
            cancelAction.OnCancelled += LoreStoreUnlocks.OnActionCancelled;
            window.AddAction(cancelAction);
            window.SetForce(5, "purchase lore items from a category or cancel the action and go back to the main menu if you dont want to purchase anything right now", "You are in the lore store for game unlocks");
            window.Register();
            return window;
        }

        public Action<PurchaseLoreItemAction> itemPurchased;

        public uiLoreStore uiLoreStore = store;
        public List<uiLoreCard> uiLoreCards = cards;
        public Dictionary<string, string> schemaData = new(_schemaData);
        public static bool isPurchasing = false;

        public override string Name => "purchase_lore_item";
        protected override string Description => "purchase an item from the store. these unlock various things that can appear in future runs.";
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            List<string> data;
            // Dictionary<string, string> schemaData = GetAllItemsDetails(uiLoreCards);
            string context = "";
            foreach (string key in schemaData.Keys)
            {
                context += $"[{key}] {StringReplace.ReplaceNewLine(schemaData[key])}.\n";
            }
            Context.Send($"Items and their descriptions you can afford: {context}");
            data = [.. schemaData.Select(l => l.Key)];
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
            if (isPurchasing)
            {
                Plugin.Logger.LogWarning("duplicate store purchase");
                return;
            }
            isPurchasing = true;
            uiLoreStore.StartCoroutine(DoPurchase(LoreStoreUnlocks.availableLoreData[parsedData]["card"] as uiLoreCard, parsedData));
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data.Value<string>("item");
            if (result == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("item"));
            if (!LoreStoreUnlocks.availableLoreData.ContainsKey(result))
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            }
            parsedData = result;
            return ExecutionResult.Success();
        }

        private IEnumerator DoPurchase(uiLoreCard card, string itemName, float delay = 1.0f)
        {
            card.Select();
            bool failedPurchase = false;
            if (card.m_LoreItem.IsPurchased())
            {
                Plugin.Logger.LogError($"card {card.m_LoreItem.m_ID} is already purchased");
                failedPurchase = true;
            }
            if (!card.m_LoreItem.CanAfford())
            {
                Plugin.Logger.LogError($"cannot afford {card.m_LoreItem.m_ID}");
                failedPurchase = true;
            }
            if (!card.m_LoreItem.IsRevealed())
            {
                Plugin.Logger.LogError($"card {card.m_LoreItem.m_ID} is not revealed");
                failedPurchase = true;
            }
            if (failedPurchase)
            {
                Context.Send($"there was an issue purchasing the store item {itemName}, going back to the main menu{NeuroSdkStrings.ModFaultSuffix}");
                uiLoreStore.OnClose();
                isPurchasing = false;
                yield break;
            }
            string successMsg = $"you purchased [{itemName}]";
            foreach (KeyValuePair<string, Dictionary<string, object>> item in LoreStoreUnlocks.availableLoreData)
            {
                if (item.Key.ToLower() != itemName.ToLower()) continue;
                successMsg = $"you purchased [{item.Key}: {item.Value["description"]}]";
                break;
            }
            Context.Send(successMsg);
            yield return new WaitForSeconds(delay);
            if (!GlobalConfig.debug_mode) card.CommitToLorePurchase(); // skips confirm popup, debug_mode skips purchase fully
            yield return new WaitForSeconds(delay);
            itemPurchased.Invoke(this);
            isPurchasing = false;
        }
    }
}


