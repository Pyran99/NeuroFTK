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

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.Show))]
        [HarmonyPostfix]
        static void OnShow(uiLoreStore __instance, List<uiLoreCard> ___m_AllCards)
        {
            Plugin.Logger.LogMessage("lore store shown");
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

            string query = "Items that can be unlocked. The categories are: " + json;
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.SetForce(2, query, "", true, ActionsForce.Priority.Low);
            window.AddAction(new PurchaseLoreItems(instance, allCards));
            window.SetContext("Purchase lore items or save for later if you dont want to purchase anything right now");
            window.Register();
            activeWindow = window;
        }

        //TODO send as context OR create actions for each category, add all to window
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
            string json = JsonConvert.SerializeObject(categoryData, Formatting.Indented);
            Plugin.Logger.LogMessage($"category data: {json}");
            return categoryData;
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
