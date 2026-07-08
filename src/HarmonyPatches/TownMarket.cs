using System.Collections;
using System.Collections.Generic;
using System.Text;
using FTKItemName;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class TownMarket
    {
        static ActionWindow activeWindow;
        static readonly Dictionary<string, uiItemIcon> buyList = [];
        // static readonly Dictionary<string, FTK_itembase.ID> sellList = [];
        static bool isCreating = false;

        [HarmonyPatch(typeof(uiBuyMenuHud), nameof(uiBuyMenuHud.EnableMenu))]
        [HarmonyPostfix]
        static void BuyMenuEnabled()
        {
            Plugin.Logger.LogMessage("market open");
            buyList.Clear();
            Object.Destroy(activeWindow);
            // sellList.Clear();
        }

        [HarmonyPatch(typeof(GeneralMenuBase), nameof(GeneralMenuBase.DisableMenu))]
        [HarmonyPrefix]
        static void ServiceClosed2(GeneralMenuBase __instance)
        {
            if (__instance == uiBuyMenuHud.Instance)
            {
                buyList.Clear();
                // sellList.Clear();
                isCreating = false;
                Object.Destroy(activeWindow);
            }
        }

        [HarmonyPatch(typeof(uiBuyMenuHud), "UpdateStockListDisplay")] // first open after full | after sell | after buy
        [HarmonyPostfix]
        static void Refresh2()
        {
            Plugin.Logger.LogMessage("market stock refresh");
            Object.Destroy(activeWindow);
            uiBuyMenuHud.Instance.StartCoroutine(AddData(null));
        }

        static IEnumerator AddData(ItemContainer _itemContainer)
        {
            if (isCreating) yield break;
            isCreating = true;
            CharacterOverworld _cow = GameLogic.Instance.GetCurrentCOW();
            if (_itemContainer == _cow.m_PlayerInventory.m_ContainerBackpack)
            {
                Plugin.Logger.LogWarning("sell list. unsure when called");
                yield break;
            }
            // make sure old objects removed & texts updated
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            Transform list = uiBuyMenuHud.Instance.GetListRootTransform();
            buyList.Clear();
            foreach (Transform child in list)
            {
                uiItemIcon _item = child.GetComponent<uiItemIcon>();
                if (!_item) continue;
                if (buyList.ContainsKey(_item.m_NameText.text))
                {
                    Plugin.Logger.LogError("duplicate keys in buy list");
                    continue;
                }
                // this.m_NameText.text = this.m_ItemInfo.GetLocalizedName() + "(" + this.GetItemCount().ToString() + ")";
                if (!_cow.m_CharacterStats.CanAfford(int.Parse(_item.m_CostText.text))) continue;
                buyList.Add(_item.m_NameText.text, _item);
            }
            Context.Send($"{_cow.m_CharacterStats.m_CharacterName} has {_cow.m_CharacterStats.m_Gold.ToString() ?? "0"} gold.");
            StringBuilder sb = new();
            sb.Append("[market items ([name] description (cost))] ");
            Plugin.Logger.LogWarning("4.1");
            foreach (uiItemIcon _item in buyList.Values)
            {
                // string name = _item.m_NameText.text;
                // string cost = _item.m_CostText.text;
                // sb.AppendLine($"{name}: {cost}.");
                sb.AppendLine($"[{ItemData.GetItemName(_item.m_ItemName)}] {ItemData.GetItemDescription(_item.m_ItemName, true, _cow)} (cost {_item.m_CostText?.text} gold)");
            }
            Plugin.Logger.LogWarning("5");
            Object.Destroy(activeWindow);
            QuickTimerCallback timer = new(() => CreateAction(sb.ToString()), list.gameObject);
            isCreating = false;
        }

        static void CreateAction(string ctx)
        {
            activeWindow = ActionWindow.Create(uiBuyMenuHud.Instance.GetListRootTransform().gameObject);
            activeWindow.SetContext(ctx);
            activeWindow.AddAction(new MarketPurchaseAction(new(buyList)));
            // TODO sell action
            CancelAction cancel = new(activeWindow, "close the market");
            cancel.OnCancelled += CloseMenu;
            activeWindow.AddAction(cancel);
            activeWindow.SetForce(0, "buy/sell items at the market or close the menu if there is nothing you want", "you are at the market");
            activeWindow.Register();
        }

        public static void NeuroDecision(uiItemIcon _item, bool equip)
        {
            // SelectButton.StartCoroutine(_item);
            _item.StartCoroutine(BuyCoroutine(_item, equip));
        }

        public static void CloseMenu(ActionWindow window = null)
        {
            uiFTKButton closeBtn = uiBuyMenuHud.Instance.transform.Find("buyMenuMain")?.transform.Find("closeButton")?.GetComponent<uiFTKButton>();
            if (closeBtn == null)
            {
                Plugin.Logger.LogError("market close btn is null");
                return;
            }
            SelectButton.StartCoroutine(closeBtn, 1.0f);
        }

        static IEnumerator BuyCoroutine(uiFTKButton btn, bool equip)
        {
            btn.OnPointerEnter(null);
            yield return new WaitForSeconds(0.25f);
            btn.Select();
            yield return new WaitForSeconds(0.25f);
            // brings up menu => uiPopupMenu
            if (equip)
            {
                uiBuyMenuHud.Instance.BuyAndEquipCurrentItem();
            }
            else
            {
                uiBuyMenuHud.Instance.BuyCurrentItem();
            }
        }
    }
}