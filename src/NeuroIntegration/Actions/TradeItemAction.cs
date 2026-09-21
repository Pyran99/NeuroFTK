using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class TradeItem
    {
        public static void SendItemTo(CharacterOverworld owner, CharacterOverworld target, List<FTK_itembase.ID> item)
        {
            Plugin.Logger.LogMessage($"sending {string.Join(", ", [.. item.Select(x => ItemData.GetItemName(x))])} to {CharacterData.GetCharacterName(target)}");
            owner.m_SelectedBackpackCategory = uiPlayerInventory.BackpackCatagory.All;
            uiPlayerInventory.Instance.ShowCharacterInventory(owner, false);
            uiPlayerInventory.Instance.StartCoroutine(Delay(owner, target, item));
        }

        static uiItemIcon GetBackpackItem(CharacterOverworld owner, FTK_itembase.ID itemToSend)
        {
            uiItemIcon result = null;
            if (owner.m_PlayerInventory.GetItemCount(PlayerInventory.ContainerID.Backpack, itemToSend) > 0)
            {
                foreach (uiItemIcon icon in uiPlayerInventory.Instance.m_BackpackUIContainer.transform.GetComponentsInChildren<uiItemIcon>())
                {
                    if (icon.m_ItemName == itemToSend)
                    {
                        result = icon;
                        break;
                    }
                }
            }
            return result;
        }

        static IEnumerator Delay(CharacterOverworld owner, CharacterOverworld target, List<FTK_itembase.ID> items)
        {
            yield return null;
            uiPopupMenu popup = FTKUI.Instance.m_PopupMenu;
            uiItemIcon icon = null;
            bool failed = false;
            foreach (FTK_itembase.ID item in items)
            {
                yield return null;
                icon = GetBackpackItem(owner, item);
                if (icon == null)
                {
                    Plugin.Logger.LogError($"could not find {ItemData.GetItemName(item)} in backpack of {CharacterData.GetCharacterName(owner)}");
                    failed = true;
                    continue;
                }
                uiPlayerInventory.Instance.SelectItemIcon(icon);
                yield return new WaitForSeconds(0.25f);
                PointerEventData pointerEventData = new(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = icon.transform.position
                };
                uiPlayerInventory.Instance.m_BackpackUIContainer.OnLeftClick(icon, pointerEventData);
                popup.m_DisplayRoot.transform.position = new Vector3(popup.m_InventoryTarget.transform.position.x, icon.transform.position.y, 0f);
                yield return new WaitForSeconds(0.25f);
                uiPopupMenuButton giveBtn = null;
                foreach (uiPopupMenuButton btn in popup.m_Buttons[uiPopupMenu.Action.Give])
                {
                    if (!btn.m_UnitySelectable.interactable) continue;
                    if (btn.m_RawImage.texture != target.GetPortraitTexture()) continue;
                    giveBtn = btn;
                    break;
                }
                if (giveBtn == null)
                {
                    Plugin.Logger.LogError("give btn null");
                    Movement.Instance.StartCoroutine(QuickTimerCallback.WaitRoutine(OverworldFlow.ResumeTurnMovement, FTKUI.Instance.m_HexStatusOverworld.gameObject, 0.5f));
                    failed = true;
                    continue;
                }
                giveBtn.OnPointerEnter(null);
                yield return new WaitForSeconds(1f);
                giveBtn.OnControllerClick();
                yield return null;
                if (popup.gameObject.GetActive()) popup.Hide();
                
            }
            if (failed) Context.Send($"{StringMessages.ActionIssueOccured.Format(["give_item"])}, not all items could be sent", true);
            Movement.Instance.StartCoroutine(QuickTimerCallback.WaitRoutine(OverworldFlow.ResumeTurnMovement, FTKUI.Instance.m_HexStatusOverworld.gameObject, 0.5f));
            uiPlayerInventory.Instance.CloseInventory();
        }
    }

    public class TradeItemAction(Dictionary<string, FTK_itembase.ID> items, Dictionary<string, CharacterOverworld> targets) : NeuroAction<object[]>
    {
        public override string Name => "give_item";
        protected override string Description => "give items to another character within range";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["target", "items"],
                Properties = new()
                {
                    ["target"] = QJS.Enum(targets.Keys),
                    ["items"] = new()
                    {
                        Type = JsonSchemaType.Array,
                        Items = QJS.Enum(items.Keys),
                        UniqueItems = true,
                        MinItems = 1,
                        MaxItems = items.Count
                    }
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            OverworldFlow.isFirstAction = false;
            OverworldFlow.isSearching = false;
            CharacterOverworld target = targets[(string)parsedData[0]];
            IEnumerable<string> _items = (IEnumerable<string>)parsedData[1];
            List<FTK_itembase.ID> items2 = [];
            foreach (string s in _items)
            {
                if (items.ContainsKey(s))
                {
                    if (items2.Contains(items[s])) continue;
                    items2.Add(items[s]);
                }
            }
            if (!items2.Any()) Plugin.Logger.LogError("no valid items in list");
            TradeItem.SendItemTo(CharacterData.GetActiveCow(), target, items2);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[2];
            string target = actionData.Data?.SelectToken("target")?.Value<string>();
            if (target.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
            if (!targets.ContainsKey(target)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("target"));
            IEnumerable<string> items = [.. actionData.Data?.SelectToken("items")?.Values<string>()];
            if (items.Count() == 0) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            parsedData[0] = target;
            parsedData[1] = items;
            return ExecutionResult.Success();
        }
    }
}