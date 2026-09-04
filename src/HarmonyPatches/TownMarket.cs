using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        static bool isCreating = false;
        static readonly Dictionary<string, uiItemIcon> buyList = [];

        [HarmonyPatch(typeof(uiBuyMenuHud), nameof(uiBuyMenuHud.EnableMenu))]
        [HarmonyPostfix]
        static void BuyMenuEnabled()
        {
            buyList.Clear();
            Object.Destroy(activeWindow);
        }

        [HarmonyPatch(typeof(GeneralMenuBase), nameof(GeneralMenuBase.DisableMenu))]
        [HarmonyPrefix]
        static void ServiceClosed2(GeneralMenuBase __instance)
        {
            if (__instance == uiBuyMenuHud.Instance)
            {
                buyList.Clear();
                isCreating = false;
                Object.Destroy(activeWindow);
            }
        }

        [HarmonyPatch(typeof(uiBuyMenuHud), "UpdateStockListDisplay")] // first open after full | after sell | after buy
        [HarmonyPostfix]
        static void Refresh2()
        {
            Object.Destroy(activeWindow);
            if (Multiplayer.OtherPlayersAction(CharacterData.GetActiveCow())) return;
            uiBuyMenuHud.Instance.StartCoroutine(AddData(null, uiBuyMenuHud.Instance.m_CurrentCow));
        }

        static IEnumerator AddData(ItemContainer _itemContainer, CharacterOverworld _cow)
        {
            if (isCreating) yield break;
            isCreating = true;
            if (_cow == null)
            {
                Plugin.Logger.LogError("market cow was not set");
                isCreating = false;
                yield break;
            }
            if (_itemContainer == _cow?.m_PlayerInventory.m_ContainerBackpack)
            {
                Plugin.Logger.LogWarning("sell list. unsure when called");
                yield break;
            }
            // make sure old objects removed & texts updated
            yield return null;
            yield return null;
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
                if (_item.m_CountText.text == "0") continue;
                if (!_cow.m_CharacterStats.CanAfford(int.Parse(_item.m_CostText.text))) continue;
                // this.m_NameText.text = this.m_ItemInfo.GetLocalizedName() + "(" + this.GetItemCount().ToString() + ")";
                buyList.Add(_item.m_NameText.text, _item);
            }
            StringBuilder sb = new();
            string name = CharacterData.GetCharacterName(_cow);
            sb.AppendLine($"{name} has {_cow.m_CharacterStats.m_Gold.ToString() ?? "0"} gold.");
            sb.AppendLine($"## {name} equipment");
            foreach (KeyValuePair<PlayerInventory.ContainerID, FTK_itembase.ID> item in CharacterData.GetAllEquipment(_cow))
            {
                if (item.Value == FTK_itembase.ID.None)
                {
                    sb.AppendLine($"- ({item.Key}) None. ");
                    continue;
                }
                sb.AppendLine($"- ({item.Key}) {ItemData.GetItemName(item.Value)} {ItemData.GetItemDescription(item.Value, _cow, true, true)}. ");
            }
            sb.AppendLine($" (they want equipment that uses or increases {CharacterData.GetClassMainStat(_cow.m_CharacterStats.m_CharacterClass)} stats).");
            Context.Send(sb.ToString());
            sb = new();
            sb.AppendLine("## market items ([name](cost) description) ");
            foreach (uiItemIcon _item in buyList.Values)
            {
                sb.AppendLine($"- [{ItemData.GetItemName(_item.m_ItemName)}]({_item.m_CostText?.text} gold) {ItemData.GetItemDescription(_item.m_ItemName, _cow, true, true)}");
            }
            Object.Destroy(activeWindow);
            uiBuyMenuHud.Instance.StartCoroutine(QuickTimerCallback.WaitRoutine(() => CreateAction(sb.ToString()), list.gameObject));
            isCreating = false;
        }

        static void CreateAction(string ctx)
        {
            activeWindow = ActionWindow.Create(uiBuyMenuHud.Instance.GetListRootTransform().gameObject);
            if (buyList.Count > 0)
            {
                activeWindow.SetContext(ctx);
                activeWindow.AddAction(new MarketPurchaseAction(new(buyList)));
            }
            else
            {
                activeWindow.SetContext("you cannot afford anything at this market (vedal should give you a raise)");
            }
            CancelAction cancel = new(activeWindow, "close the market");
            cancel.OnCancelled += CloseMenu;
            activeWindow.AddAction(cancel);
            activeWindow.SetForce(0, "buy/sell items at the market or close the menu if there is nothing you want", "", true);
            activeWindow.Register();
        }

        public static void NeuroDecision(uiItemIcon _item, bool equip)
        {
            bool _equip = equip;
            if (!_item.m_ItemInfo.m_Equippable) _equip = false;
            _item.StartCoroutine(BuyCoroutine(_item, _equip));
        }

        public static void CloseMenu(ActionWindow window = null)
        {
            uiFTKButton closeBtn = uiBuyMenuHud.Instance.transform.Find("buyMenuMain")?.transform.Find("closeButton")?.GetComponent<uiFTKButton>();
            if (closeBtn == null)
            {
                Plugin.Logger.LogError("market close btn is null");
                return;
            }
            SelectButton.StartCoroutine(closeBtn);
        }

        static IEnumerator BuyCoroutine(uiFTKButton btn, bool equip)
        {
            btn.OnPointerEnter(null);
            yield return new WaitForSeconds(0.25f);
            btn.Select();
            yield return new WaitForSeconds(0.5f);
            // skips uiPopupMenu
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