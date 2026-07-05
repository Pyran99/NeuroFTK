using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using StartGameFE;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using Newtonsoft.Json;
using System.Collections;
using NeuroSdk;
using UnityEngine;
using Pyran.NeuroFTK.GameConfigs;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class LoreStoreUnlocks
    {
        static ActionWindow activeWindow = null;
        static uiLoreStore uiLoreStore;
        static List<uiLoreCard> uiLoreCards;
        // {"night market": {"description": "", "card": LoreCard}}
        public static readonly Dictionary<string, Dictionary<string, object>> availableLoreData = [];
        public static bool isPurchasing = false;

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.Show))]
        [HarmonyPostfix]
        static void OnShow(uiLoreStore __instance, List<uiLoreCard> ___m_AllCards)
        {
            string json = JsonConvert.SerializeObject(GetCategoryData(), Formatting.None);
            json = StringReplace.ReplaceNewLine(json);
            Context.Send($"[store categories] {json}");
            isPurchasing = false;
            uiLoreStore = __instance;
            uiLoreCards = ___m_AllCards;
            CreateAction(__instance, ___m_AllCards);
        }

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.OnClose))]
        [HarmonyPrefix]
        static void OnClosed()
        {
            availableLoreData.Clear();
            uiLoreCards.Clear();
            Object.Destroy(activeWindow);
        }

        public static void OnActionCancelled(ActionWindow window)
        {
            Object.Destroy(window);
            uiLoreStore.OnClose();
        }

        public static Dictionary<string, string> GetCategoryData()
        {
            List<FTK_loreCategory> categories = [.. FTK_loreCategoryDB.GetDB().m_Array];
            List<string> names = [.. categories.Select(c => c.m_DisplayName)];
            Dictionary<string, string> categoryData = [];
            string trName = "";
            string trDescription = "";
            foreach (FTK_loreCategory category in categories)
            {
                if (categoryData.ContainsKey(category.m_DisplayName)) continue;
                trName = FTKHub.Localized<TextMisc>(category.m_DisplayName);
                trDescription = FTKHub.Localized<TextLoreStore>(category.m_CategoryDescription);
                categoryData[trName] = trDescription;
            }
            return categoryData;
        }

        public static void OnItemPurchased()
        {
            isPurchasing = false;
            Object.Destroy(activeWindow);
            if (MainMenu.GetPurchasableLoreCount() > 0)
            {
                MainMenu.HasPurchasableLore();
                CreateAction(uiLoreStore, uiLoreCards);
                return;
            }
            uiLoreStore.OnClose();
        }

        static void CreateAction(uiLoreStore _instance, List<uiLoreCard> _uiLoreCards)
        {
            availableLoreData.Clear();
            Dictionary<string, string> schemaData = GetAllItemsDetails(_uiLoreCards);
            QuickTimerCallback timer = new(() =>
            {
                activeWindow = PurchaseLoreItemAction.RegisterAction(_instance, schemaData);
                UnregisterDisabledObject.QuickCreate(uiLoreStore.gameObject, activeWindow);
            }, _instance.m_LoreRoot.gameObject, 2000f);
        }

        // get every item that can be purchased
        private static Dictionary<string, string> GetAllItemsDetails(List<uiLoreCard> cards)
        {
            Dictionary<string, string> loreData = [];
            Dictionary<string, string> entry;
            FTK_loreItem item;
            foreach (uiLoreCard card in cards)
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

        private static string FixName(string name)
        {
            return name switch
            {
                "HelmetMask01" => "HelmetBeastman",
                "HelmetMask02" => "HelmetOwlbear",
                "HelmetMask03" => "HelmetTriclops",
                _ => name,
            };
        }

        public static IEnumerator DoPurchase(uiLoreCard card, string itemName, float delay = 1.0f)
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
            foreach (KeyValuePair<string, Dictionary<string, object>> item in availableLoreData)
            {
                if (item.Key.ToLower() != itemName.ToLower()) continue;
                successMsg = $"you purchased [{item.Key}: {item.Value["description"]}]";
                break;
            }
            Context.Send(successMsg);
            yield return new WaitForSeconds(delay);
            if (!GlobalConfig.debug_mode) card.CommitToLorePurchase(); // skips confirm popup, debug_mode skips purchase fully
            yield return new WaitForSeconds(delay);
            OnItemPurchased();
            isPurchasing = false;
        }


        // // test idea to put each category into its own action
        // static void NewActionsTest(uiLoreStore instance, List<uiLoreCard> allCards)
        // {
        //     List<FTK_loreCategory> categories = [.. FTK_loreCategoryDB.GetDB().m_Array];
        //     ActionWindow window = ActionWindow.Create(instance.gameObject);
        //     Dictionary<string, string> categoryData = GetCategoryData();
        //     string context = "";
        //     foreach (string Key in categoryData.Keys)
        //     {
        //         FTK_loreCategory.ID id = FTK_loreCategory.ID.None;
        //         foreach (FTK_loreCategory cat in categories)
        //         {
        //             if (cat.m_DisplayName == Key)
        //             {
        //                 id = FTK_loreCategory.GetEnum(cat.m_ID);
        //                 break;
        //             }
        //         }
        //         PurchaseLoreItemCategoryAction purchaseLoreItemTest = new(instance, allCards, Key, id);
        //         window.AddAction(purchaseLoreItemTest);
        //         context += $"{{{Key}: {categoryData[Key]}}},";
        //     }
        //     window.SetForce(3, "Purchase lore items from a category or save for later if you dont want to purchase anything right now", "You are in the lore store", true, ActionsForce.Priority.Low);
        //     window.SetContext(context);
        //     window.Register();
        // }

    }
}


