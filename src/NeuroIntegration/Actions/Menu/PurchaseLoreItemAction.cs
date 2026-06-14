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
using Newtonsoft.Json;
using Pyran.NeuroFTK.GameConfigs;
using StartGameFE;
using UnityEngine;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItemAction(uiLoreStore store, List<uiLoreCard> cards) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(uiLoreStore instance, List<uiLoreCard> cards)
        {
            string json = JsonConvert.SerializeObject(LoreStoreUnlocks.GetCategoryData(), Formatting.None);
            json = StringReplace.ReplaceNewLine(json);
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            PurchaseLoreItemAction action = new(instance, cards);
            action.itemPurchased += LoreStoreUnlocks.OnItemPurchased;
            window.SetContext($"lore store category details: {json}");
            window.SetForce(4, "purchase lore items from a category or cancel the action and go back to the main menu if you dont want to purchase anything right now", "You are in the lore store for game unlocks");
            window.AddAction(action);
            CancelAction cancelAction = new(window, "return to main menu");
            cancelAction.OnCancelled += LoreStoreUnlocks.OnActionCancelled;
            window.AddAction(cancelAction);
            window.Register();
            return window;
        }

        public uiLoreStore uiLoreStore = store;
        public List<uiLoreCard> uiLoreCards = cards;
        public Action<PurchaseLoreItemAction> itemPurchased;
        public static bool isPurchasing = false;
        // {"night market": {"description": "", "card": LoreCard}}
        readonly Dictionary<string, Dictionary<string, object>> availableLoreData = [];

        public override string Name => "purchase_lore_item";
        protected override string Description => "purchase an item from the store. these unlock various things that can appear in future runs.";
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            List<string> data;
            Dictionary<string, string> schemaData = GetAllItemsDetails(uiLoreCards);
            string context = "";
            foreach (string key in schemaData.Keys)
            {
                context += $"{{name: {key}, description: {StringReplace.ReplaceNewLine(schemaData[key])}}}. ";
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
            uiLoreStore.StartCoroutine(DoPurchase(availableLoreData[parsedData]["card"] as uiLoreCard, parsedData));
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            if (!actionData.Data.Contains("item")) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(["item"]));
            string result = actionData.Data.Value<string>("item");
            if (!availableLoreData.ContainsKey(result))
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
                Plugin.Logger.LogWarning($"card {card.m_LoreItem.m_ID} is already purchased");
                failedPurchase = true;
            }
            if (!card.m_LoreItem.CanAfford())
            {
                Plugin.Logger.LogWarning($"cannot afford {card.m_LoreItem.m_ID}");
                failedPurchase = true;
            }
            if (!card.m_LoreItem.IsRevealed())
            {
                Plugin.Logger.LogWarning($"card {card.m_LoreItem.m_ID} is not revealed");
                failedPurchase = true;
            }
            if (failedPurchase)
            {
                Context.Send($"there was an issue purchasing the store item {itemName}, going back to the main menu{NeuroSdkStrings.ModFaultSuffix}");
                uiLoreStore.OnClose();
                isPurchasing = false;
                yield break;
            }
            string successMsg = $"you purchased {itemName}";
            foreach (KeyValuePair<string, Dictionary<string, object>> item in availableLoreData)
            {
                if (item.Key.ToLower() != itemName.ToLower()) continue;
                successMsg = $"you purchased: '{item.Key}' '{item.Value["description"]}'";
                break;
            }
            Context.Send(successMsg);
            yield return new WaitForSeconds(delay);
            if (!GlobalConfig.debug_mode) card.CommitToLorePurchase(); // skips confirm popup, debug_mode skips purchase fully
            yield return new WaitForSeconds(delay);
            itemPurchased.Invoke(this);
            isPurchasing = false;
        }

        // get every item that can be purchased
        private Dictionary<string, string> GetAllItemsDetails(List<uiLoreCard> cards)
        {
            Dictionary<string, string> loreData = [];
            Dictionary<string, string> entry;
            FTK_loreItem item;
            foreach (uiLoreCard card in uiLoreCards)
            {
                item = card.m_LoreItem;
                if (!item.IsRevealed() || item.IsPurchased() || !item.CanAfford()) continue;
                if (item.m_Category != FTK_loreCategory.ID.items)
                {
                    // ShowOtherLoreItem
                    entry = LoreItemData.GetItemIdAndDescription(item);
                }
                else
                {
                    // this.m_ItemDetail.Show(_itemID, uiItemDetail.Mode.ItemDisplay, _cow, false, _forceFrontSide, _loreCard);
                    FTK_itembase itemBase = FTK_itembase.GetItemBase((FTK_itembase.ID)item.m_UnlockID);
                    // string trName = itemBase.GetLocalizedName();
                    entry = LoreItemData.HandleEquipmentDetails((FTK_itembase.ID)item.m_UnlockID);
                }
                string key = entry.Keys?.First().ToLower();
                // some item sets use the same name
                if (item.m_Category == FTK_loreCategory.ID.extraArmor || item.m_Category == FTK_loreCategory.ID.extraBackpack || item.m_Category == FTK_loreCategory.ID.extraHelmet || item.m_Category == FTK_loreCategory.ID.extraSkin)
                {
                    key = item.m_ID;
                }
                if (loreData.ContainsKey(key))
                {
                    Plugin.Logger.LogWarning($"duplicate key found {key}");
                    continue;
                }
                key = FixName(key);
                string value = entry.Values?.First();
                loreData.Add(key, value);
                Dictionary<string, object> _value = new()
                {
                    {"description", value},
                    {"card", card}
                };
                availableLoreData.Add(key, _value);
            }
            return loreData;
        }

        private string FixName(string name)
        {
            return name switch
            {
                "HelmetMask01" => "HelmetBeastman",
                "HelmetMask02" => "HelmetOwlbear",
                "HelmetMask03" => "HelmetTriclops",
                _ => name,
            };
        }

    }
}


