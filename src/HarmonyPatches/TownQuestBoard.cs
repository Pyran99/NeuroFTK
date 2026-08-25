using System.Collections;
using System.Collections.Generic;
using System.Text;
using FTKItemName;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class TownQuestBoard
    {
        static ActionWindow window;
        static readonly List<QuestListItem> items = [];
        static readonly Dictionary<string, QuestListItem> itemsById = [];


        [HarmonyPatch(typeof(uiGetQuestMenu), nameof(uiGetQuestMenu.EnableMenu))]
        [HarmonyPostfix]
        static void BoardOpened(MiniHexInfo _hex)
        {
            uiGetQuestMenu.Instance.StartCoroutine(Wait());
            static IEnumerator Wait()
            {
                yield return new WaitForEndOfFrame();
                if (uiGetQuestMenu.Instance.m_NoQuestDisplay.text != string.Empty)
                {
                    Context.Send("no quests available at this location");
                    SelectButton.StartCoroutine(uiGetQuestMenu.Instance.m_BackButton.GetComponent<uiFTKButton>(), 2.5f);
                    yield break;
                }
                uiGetQuestMenu.Instance.StartCoroutine(GetQuestItems());
            }
        }

        [HarmonyPatch(typeof(uiGetQuestMenu), nameof(uiGetQuestMenu.OnClose))]
        [HarmonyPrefix]
        static void QuestMenuClosed()
        {
            items.Clear();
            itemsById.Clear();
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(GeneralMenuBase), nameof(GeneralMenuBase.DisableMenu))]
        [HarmonyPrefix]
        static void ServiceClosed2(GeneralMenuBase __instance)
        {
            if (__instance == uiGetQuestMenu.Instance)
            {
                items.Clear();
                itemsById.Clear();
                Object.Destroy(window);
            }
        }

        static IEnumerator GetQuestItems()
        {
            yield return null; // TODO make sure not broken
            items.Clear();
            itemsById.Clear();
            Transform root = uiGetQuestMenu.Instance.m_ListRoot.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                GameObject child = root.GetChild(i).gameObject;
                QuestListItem item = child.GetComponent<QuestListItem>();
                if (item)
                {
                    items.Add(item);
                }
            }
            string ctx = "";
            StringBuilder sb = new();
            for (int i = 0; i < items.Count; i++)
            {
                itemsById.Add($"quest {i+1}", items[i]);
                sb.Append($"[quest {i + 1}] ");
                sb.Append($"{StringReplace.RemoveStyling(items[i].m_Quest.GetLocalizedOneLineDesc())}. Reward {items[i].m_Quest.GetRewardString()}");
                if (items[i].m_Quest.m_RewardType == QuestLogicBase.Reward.Item)
                {
                    sb.Append(GetItemDetails(i));
                }
                sb.AppendLine();
            }
            ctx = sb.ToString();
            Context.Send(ctx);
            QuickTimerCallback timer = new(CreateAction, uiGetQuestMenu.Instance.m_ListRoot.gameObject);
        }

        static string GetItemDetails(int count)
        {
            string result = "";
            FTK_itembase.ID id = items[count].m_Quest.m_RewardItem;
            FTK_itembase itemBase = FTK_itembase.GetItemBase(id);
            result += $" ({FTKHub.Localized<TextMisc>(FTK_itemRarityLevelDB.GetDB().GetEntry(itemBase.m_ItemRarity).m_Display)}),";
            if (itemBase is FTK_items)
            {
                FTKItem _item = FTKItem.Get(id);
                result += $" {_item.GetDescription(CharacterData.GetActiveCow())}";
            }
            else if (itemBase is FTK_weaponStats2 stats)
            {
                result += ItemData.GetItemDescription(id, true, CharacterData.GetActiveCow());
            }
            return result;
        }

        static void CreateAction()
        {
            window = TownQuestBoardAction.CreateWindow(itemsById);
            UnregisterDisabledObject.QuickCreate(uiGetQuestMenu.Instance.m_ListRoot.gameObject, window, true);
        }

        public static void NeuroDecision(QuestListItem _item)
        {
            Context.Send("Quest accepted: " + StringReplace.RemoveStyling(_item.m_QuestDetail?.m_Text.text));
            Button btn = _item.m_Button;
            uiFTKButton btnComp = btn.GetComponent<uiFTKButton>();
            if (btnComp != null && !_item.m_QuestDetail.gameObject.activeSelf) SelectButton.StartCoroutine(btnComp, 0.25f);
            _item.StartCoroutine(AcceptQuest(_item.m_QuestDetail));
        }

        static IEnumerator AcceptQuest(uiQuestDetail _detail)
        {
            yield return new WaitForSeconds(0.3f);
            GameObject obj = uiGetQuestMenu.Instance.m_ListRoot.gameObject;
            while (!_detail.gameObject.activeInHierarchy)
            {
                if (!obj.GetActive())
                {
                    Plugin.Logger.LogError("quest menu was closed");
                    Context.Send("there was an issue accepting the quest", true);
                    yield break;
                }
                yield return null;
            }
            yield return new WaitForSeconds(2.5f);
            SelectButton.StartUnityBtnCoroutine(_detail.m_AcceptButton);
        }
        
    }
}