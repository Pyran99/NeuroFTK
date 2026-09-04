using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using StartGameFE;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using System.Collections;
using NeuroSdk;
using UnityEngine;
using Pyran.NeuroFTK.GameConfigs;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class LoreStoreUnlocks
    {
        public static uiLoreStore uiLoreStore;
        public static bool isPurchasing = false;
        public static bool skipCustomization = false;
        
        static ActionWindow activeWindow = null;
        static List<uiLoreCard> uiLoreCards;

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.Show))]
        [HarmonyPostfix]
        static void OnShow(uiLoreStore __instance, List<uiLoreCard> ___m_AllCards)
        {
            isPurchasing = false;
            uiLoreStore = __instance;
            uiLoreCards = ___m_AllCards;
            CreateAction(__instance, ___m_AllCards);
        }

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.OnClose))]
        [HarmonyPrefix]
        static void OnClosed()
        {
            uiLoreCards.Clear();
            Object.Destroy(activeWindow);
        }

        public static bool HasPurchasableLore()
        {
            int lorePoints = LorePersistence.Instance.GetLore();
            int purchasableItemsCount = GetPurchasableLoreCount();
            Context.Send($"You have {lorePoints} lore points and there are {purchasableItemsCount} items you can afford. These can be used in the lore store to unlock new events, characters, equipment, and cosmetics, if you have enough points, or saved for another time.");
            return purchasableItemsCount > 0;
        }

        public static int GetPurchasableLoreCount()
        {
            int purchasableItemsCount = 0;
            foreach (FTK_loreItem loreItem in FTK_loreItemDB.GetDB().m_Array)
            {
                if (!LoreItemData.IsAvailable(loreItem)) continue;
                purchasableItemsCount += 1;
            }
            return purchasableItemsCount;
        }

        public static void OnActionCancelled(ActionWindow window)
        {
            Object.Destroy(window);
            uiLoreStore.OnClose();
        }

        public static void OnItemPurchased()
        {
            isPurchasing = false;
            Object.Destroy(activeWindow);
            if (GetPurchasableLoreCount() > 0)
            {
                HasPurchasableLore();
                CreateAction(uiLoreStore, uiLoreCards);
                return;
            }
            uiLoreStore.OnClose();
        }

        static void CreateAction(uiLoreStore _instance, List<uiLoreCard> _uiLoreCards)
        {
            Dictionary<FTK_loreCategory, List<uiLoreCard>> categoryData = GenerateCardList(_uiLoreCards);
            // _instance.StartCoroutine(ActionWait(_instance, categoryData)); // potential #58 fix
            _instance.StartCoroutine(QuickTimerCallback.WaitRoutine(() =>
            {
                activeWindow = PurchaseLoreItemAction.RegisterAction(_instance, categoryData);
                UnregisterDisabledObject.QuickCreate(uiLoreStore.m_LoreRoot.gameObject, activeWindow);
            }, _instance.m_LoreRoot.gameObject));
        }

        public static Dictionary<FTK_loreCategory, List<uiLoreCard>> GenerateCardList(List<uiLoreCard> cards)
        {
            Dictionary<FTK_loreCategory, List<uiLoreCard>> categoryData = [];
            foreach (FTK_loreCategory category in FTK_loreCategoryDB.GetDB().m_Array)
            {
                if (category.m_DisplayName == "STR_realms") continue; // no realm category data
                categoryData[category] = [];
            }
            FTK_loreItem item;
            FTK_loreCategory cat;
            foreach (uiLoreCard card in cards)
            {
                item = card.m_LoreItem;
                if (!LoreItemData.IsAvailable(item)) continue;
                cat = FTK_loreCategoryDB.Get(item.m_Category);
                categoryData[cat].Add(card);
            }
            return categoryData;
        }

        static IEnumerator ActionWait(uiLoreStore _instance, Dictionary<FTK_loreCategory, List<uiLoreCard>> categoryData)
        {
            yield return new WaitForSeconds(1.0f);
            activeWindow = PurchaseLoreItemAction.RegisterAction(_instance, categoryData);
            UnregisterDisabledObject.QuickCreate(uiLoreStore.m_LoreRoot.gameObject, activeWindow);
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
            string successMsg = $"you purchased {itemName}: {LoreItemData.GetItemDescription(card.m_LoreItem)}";
            Context.Send(successMsg);
            yield return new WaitForSeconds(delay);
            if (!GlobalConfig.IsDebugMode()) card.CommitToLorePurchase(); // skips confirm popup, debugMode skips purchase fully
            yield return new WaitForSeconds(delay);
            OnItemPurchased();
            isPurchasing = false;
        }

        // public static Dictionary<string, string> GetCategoryData()
        // {
        //     List<FTK_loreCategory> categories = [.. FTK_loreCategoryDB.GetDB().m_Array];
        //     List<string> names = [.. categories.Select(c => c.m_DisplayName)];
        //     Dictionary<string, string> categoryData = [];
        //     string trName = "";
        //     string trDescription = "";
        //     foreach (FTK_loreCategory category in categories)
        //     {
        //         if (categoryData.ContainsKey(category.m_DisplayName)) continue;
        //         trName = FTKHub.Localized<TextMisc>(category.m_DisplayName);
        //         trDescription = FTKHub.Localized<TextLoreStore>(category.m_CategoryDescription);
        //         categoryData[trName] = trDescription;
        //     }
        //     return categoryData;
        // }


        // // WORKING ON REMOVE
        // // get every item that can be purchased
        // private static Dictionary<string, string> GetAllItemsDetails(List<uiLoreCard> cards)
        // {
        //     Dictionary<string, string> loreData = [];
        //     Dictionary<string, string> entry;
        //     FTK_loreItem item;
        //     foreach (uiLoreCard card in cards)
        //     {
        //         item = card.m_LoreItem;
        //         if (!LoreItemData.IsAvailable(item)) continue;
        //         if (item.m_Category != FTK_loreCategory.ID.items)
        //         {
        //             // ShowOtherLoreItem
        //             entry = LoreItemData.GetItemIdAndDescription(item);
        //         }
        //         else
        //         {
        //             // FTK_itembase itemBase = FTK_itembase.GetItemBase((FTK_itembase.ID)item.m_UnlockID);
        //             entry = ItemData.HandleEquipmentDetails((FTK_itembase.ID)item.m_UnlockID);
        //         }
        //         string key = entry.Keys?.First().ToLower();
        //         // some item sets use the same name
        //         if (item.m_Category == FTK_loreCategory.ID.extraArmor || item.m_Category == FTK_loreCategory.ID.extraBackpack || item.m_Category == FTK_loreCategory.ID.extraHelmet || item.m_Category == FTK_loreCategory.ID.extraSkin)
        //         {
        //             key = item.m_ID;
        //         }
        //         if (loreData.ContainsKey(key))
        //         {
        //             Plugin.Logger.LogWarning($"duplicate key found {key}");
        //             continue;
        //         }
        //         key = FixName(key);
        //         string value = entry.Values?.First();
        //         loreData.Add(key, value);
        //         Dictionary<string, object> _value = new()
        //         {
        //             {"description", value},
        //             {"card", card}
        //         };
        //         availableLoreData.Add(key, _value);
        //     }
        //     return loreData;
        // }


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


