using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
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
            activeWindow = PurchaseLoreItems.RegisterAction(uiLoreStore, ___m_AllCards);
            UnregisterDisabledObject.QuickCreate(uiLoreStore.gameObject, activeWindow);
        }

        [HarmonyPatch(typeof(uiLoreStore), nameof(uiLoreStore.OnClose))]
        [HarmonyPrefix]
        static void OnClosed()
        {
            UnityEngine.Object.Destroy(activeWindow);
        }

        public static void OnActionCancelled(ActionWindow window)
        {
            UnityEngine.Object.Destroy(window);
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

        public static void OnItemPurchased(PurchaseLoreItems action)
        {
            PurchaseLoreItems.isPurchasing = false;
            // action.itemPurchased -= OnItemPurchased;
            // UnityEngine.Object.Destroy(activeWindow);
            if (MainMenu.GetPurchasableLoreCount() > 0)
            {
                MainMenu.HasPurchasableLore();
                activeWindow = PurchaseLoreItems.RegisterAction(action.uiLoreStore, action.uiLoreCards);
                UnregisterDisabledObject.QuickCreate(uiLoreStore.gameObject, activeWindow);
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
// }

