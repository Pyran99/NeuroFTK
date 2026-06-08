using System;
using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Newtonsoft.Json;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    [HarmonyPatch]
    public class LoreStoreUnlocks
    {
        static ActionWindow activeWindow = null;
        static uiLoreStore uiLoreStore;

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.Show))]
        [HarmonyPostfix]
        static void OnShow(uiLoreStore __instance, List<uiLoreCard> ___m_AllCards)
        {
            PurchaseLoreItems.isPurchasing = false;
            uiLoreStore = __instance;
            CreateNeuroAction(__instance, ___m_AllCards);
        }

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.OnClose))]
        [HarmonyPrefix]
        static void OnClosed()
        {
            UnityEngine.Object.Destroy(activeWindow);
        }

        static void CreateNeuroAction(uiLoreStore instance, List<uiLoreCard> allCards)
        {
            string json = JsonConvert.SerializeObject(GetCategoryData(), Formatting.None);
            json.Replace(@"\n", ", ");
            PurchaseLoreItems action = new(instance, allCards);
            action.itemPurchased += OnItemPurchased;
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.SetForce(2, "purchase lore items from a category or cancel the action and go back to the main menu if you dont want to purchase anything right now", "You are in the lore store for game unlocks", true, ActionsForce.Priority.Low);
            window.AddAction(action);
            CancelAction cancelAction = new("purchase_lore_item", "returning to the main menu");
            cancelAction.OnCancelled += OnActionCancelled;
            window.AddAction(cancelAction);
            window.SetContext($"lore store category details: {json}");
            window.Register();
            activeWindow = window;
        }

        static void OnActionCancelled(NeuroAction action)
        {
            NeuroActionHandler.UnregisterActions(action);
            uiLoreStore.OnClose();
        }

        static Dictionary<string, string> GetCategoryData()
        {
            List<FTK_loreCategory> categories = [.. FTK_loreCategoryDB.GetDB().m_Array];
            List<string> names = [.. categories.Select(c => c.m_DisplayName)];
            Dictionary<string, string> categoryData = [];
            TextMiscRow miscRow = null;
            TextLoreStoreRow loreStoreRow = null;
            string trName = "";
            string trDescription = "";
            foreach (FTK_loreCategory category in categories)
            {
                if (categoryData.ContainsKey(category.m_DisplayName)) continue;
                miscRow = TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), category.m_DisplayName)];
                trName = miscRow.GetStringDataByIndex(0);
                loreStoreRow = TextLoreStore.Instance.Rows[(int)Enum.Parse(typeof(TextLoreStore.rowIds), category.m_CategoryDescription)];
                trDescription = loreStoreRow.GetStringDataByIndex(0);
                categoryData[trName] = trDescription;
            }
            return categoryData;
        }

        static void OnItemPurchased(PurchaseLoreItems action)
        {
            PurchaseLoreItems.isPurchasing = false;
            // action.itemPurchased -= OnItemPurchased;
            // UnityEngine.Object.Destroy(activeWindow);
            if (MainMenu.GetPurchasableLoreCount() > 0)
            {
                MainMenu.HasPurchasableLore();
                CreateNeuroAction(action.uiLoreStore, action.uiLoreCards);
                return;
            }
            uiLoreStore.OnClose();
        }

        // test idea to put each category into its own action
        static void NewActionsTest(uiLoreStore instance, List<uiLoreCard> allCards)
        {
            List<FTK_loreCategory> categories = [.. FTK_loreCategoryDB.GetDB().m_Array];
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            Dictionary<string, string> categoryData = GetCategoryData();
            string context = "";
            foreach (string Key in categoryData.Keys)
            {
                FTK_loreCategory.ID id = FTK_loreCategory.ID.None;
                foreach (FTK_loreCategory cat in categories)
                {
                    if (cat.m_DisplayName == Key)
                    {
                        id = FTK_loreCategory.GetEnum(cat.m_ID);
                        break;
                    }
                }
                PurchaseLoreItemTest purchaseLoreItemTest = new(instance, allCards, Key, id);
                window.AddAction(purchaseLoreItemTest);
                context += $"{{{Key}: {categoryData[Key]}}},";
            }
            window.SetForce(3, "Purchase lore items from a category or save for later if you dont want to purchase anything right now", "You are in the lore store", true, ActionsForce.Priority.Low);
            window.SetContext(context);
            window.Register();
        }

    }
}


// public void OnClick()
// {
//     if (this.m_LoreItem.IsRevealed() && !this.m_LoreItem.IsPurchased())
//     {
//         if (this.m_LoreItem.CanAfford())
//         {
//             this.m_Owner.m_ConfirmHud.InitializeLoreSpend(this);
//         }
//         else
//         {
//             this.m_Owner.m_ConfirmHud.InitializeLoreCantAfford(this);
//         }
//     }
// }

// public void UnpurchaseAll()
// {
//     foreach (FTK_loreItem ftk_loreItem in FTK_loreItemDB.GetDB().m_Array)
//     {
//         FTK_statistic statistic = ftk_loreItem.GetStatistic();
//         global::StatsAchievements.StatsAchievements.TryPlayerStatisticSetValue(statistic.m_ID, 0);
//     }
// }

// FTK_statistic statistic = ftk_loreItem.GetStatistic();
// FTK_statistic ftk_statistic = FTK_statisticDB.Get(FTK_statistic.GetEnum(statistic.m_RevealStat));

// public List<string> m_UnlockedItems = new List<string>();

// [Info   : Unity Log] Lorestore item ArmorStPatrick available == True


// Dictionary<string, List<string>> purchasableItems = [];
// foreach (FTK_loreItem loreItem in FTK_loreItemDB.GetDB().m_Array)
// {
//     if (loreItem.IsPurchased()) continue;
//     if (!loreItem.IsRevealed()) continue;
//     if (loreItem.CanAfford())
//     {
//         if (!purchasableItems.ContainsKey(loreItem.m_Category.ToString()))
//         {
//             purchasableItems[loreItem.m_Category.ToString()] = [];
//             purchasableItems.Add(FTK_loreCategory.GetEnum());
//         }
//         purchasableItems[loreItem.m_Category.ToString()].Add(loreItem.m_ID);
//         FTK_loreCategory.ID.classes.ToString();
//     }
// }
    // List<FTK_loreItem> allLoreItems = FTK_loreItemDB.GetDB().GetArray();
