using System.Collections.Generic;
using System.Text;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CharacterDecisionButtons
    {
        // {character: valid buttons}
        public static readonly Dictionary<CharacterOverworld, List<VoteButton>> voteButtons = [];
        public static VoteButtonContainer instance;

        static bool isShowing = false;
        static bool addItemUse = false;
        static ActionWindow activeWindow;
        static readonly List<VoteButtonContainer> activeContainers = [];

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Show))] // called for each available character
        [HarmonyPostfix]
        static void VoteContainerShow(VoteButtonContainer __instance)
        {
            CharacterOverworld cow = __instance.m_PlayerHud.m_Cow;
            if (!Multiplayer.IsYourCow(cow)) return;
            activeContainers.Add(__instance);
            string name = CharacterData.GetCharacterName(cow);
            voteButtons[cow] = [];
            VoteButton[] btns = __instance.GetComponentsInChildren<VoteButton>();
            foreach (VoteButton btn in btns)
            {
                if (btn != null) voteButtons[cow].Add(btn);
            }
            if (isShowing) return;
            isShowing = true;
            instance = __instance;
            Object.Destroy(activeWindow);
            __instance.StartCoroutine(QuickTimerCallback.WaitRoutine(CreateAction, __instance.gameObject));
        }

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Hide))]
        [HarmonyPrefix]
        static void VoteContainerHide(VoteButtonContainer __instance)
        {
            activeContainers.Remove(__instance);
            if (activeContainers.Count > 0) return;
            voteButtons.Clear();
            isShowing = false;
            instance = null;
            Object.Destroy(activeWindow);
        }

        public static void ResetData()
        {
            activeContainers.Clear();
            voteButtons.Clear();
            isShowing = false;
            instance = null;
            Object.Destroy(activeWindow);
        }

        static void CreateAction()
        {
            activeWindow = ActionWindow.Create(instance.gameObject);
            StringBuilder sbState = new();
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                activeWindow.AddAction(new CharacterDecisionAction(kvp.Key, CharacterData.GetCharacterName(kvp.Key), kvp.Value));
                sbState.AppendLine($"{CharacterData.GetDataFor(kvp.Key)} ");
            }
            sbState.Append($"{StringMessages.FocusDetails}");
            activeWindow.SetForce(0, StringMessages.DecisionButtonsPrompt.Format(instance.m_Prompt.text), sbState.ToString(), true);
            StringBuilder sb = new(DungeonEncounterRolls());
            EncounterData encounter = EncounterSessionMC.Instance.GetCurrentEncounter();
            if (encounter != null)
            {
                MiniHexDungeon.EncounterType _encounterType = encounter.EncounterType;
                if (_encounterType == MiniHexDungeon.EncounterType.Next || _encounterType == MiniHexDungeon.EncounterType.Ready || _encounterType == MiniHexDungeon.EncounterType.Stair || _encounterType == MiniHexDungeon.EncounterType.EmptyRoom)
                {
                    Plugin.Logger.LogWarning($"change equipment in dungeon check {_encounterType}");
                    StringBuilder sb2 = new();
                    foreach (CharacterDummy dummy in EncounterSession.Instance.m_PlayerDummies.Values)
                    {
                        if (!dummy.m_CharacterOverworld) continue;
                        if (!dummy.m_IsAlive) continue;
                        StringBuilder equipSb = new();
                        Dictionary<PlayerInventory.ContainerID, List<FTK_itembase.ID>> equippableItems = [];
                        List<PlayerInventory.ContainerID> emptyContainers = CharacterData.GetEmptyContainers(dummy.m_CharacterOverworld);
                        equippableItems = EquipmentManager.GetEquippableItems(dummy.m_CharacterOverworld, emptyContainers, out string context);
                        equipSb.Append(context);

                        if (equippableItems.Count > 0)
                        {
                            activeWindow.AddAction(new ChangeEquipmentAction(EquipmentManager.GetEquipDictionary(equippableItems), dummy.m_CharacterOverworld));
                            string name = CharacterData.GetCharacterName(dummy.m_CharacterOverworld);
                            sb2.AppendLine($"## {name} has empty equipment slots, these items can be equipped to them. ");
                            sb2.AppendLine(equipSb.ToString());
                            sb2.AppendLine($"{name} prefers {CharacterData.GetClassMainStat(dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterClass)} stats, avoid equipping items that reduce them (if 'any' you can choose what stats to avoid).");
                        }
                    }
                    Context.Send(sb2.ToString());
                }
            }

            if (sb.Length != 0)
            {
                if (CombatUtils.Entry != null)
                {
                    sb.Append(StringMessages.RollSkillType.Format(ItemData.SwitchSkillTestName(CombatUtils.Entry.m_TestSkill)));
                }
                activeWindow.SetContext(sb.ToString());
            }
            if (addItemUse) // unfinished
            {
                foreach (CharacterDummy dummy in EncounterSession.Instance.m_PlayerDummies.Values)
                {
                    if (!dummy.m_CharacterOverworld) continue;
                    if (!dummy.m_IsAlive) continue;
                    // List<FTK_itembase.ID> items = ItemData.GetUsableBeltItems(dummy.m_CharacterOverworld);
                    // Dictionary<string, FTK_itembase.ID> items2 = items.ToDictionary(ItemData.GetItemName, x => x);
                    // if (items.Count > 0) activeWindow.AddAction(new UseBeltItemAction(items2, dummy.m_CharacterOverworld));
                }
            }
            activeWindow.Register();
        }

        static string DungeonEncounterRolls()
        {
            StringBuilder sb = new();
            string detail = StringMessages.DungeonRolls;
            sb.AppendLine(detail);
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                CharacterOverworld cow = kvp.Key;
                sb.AppendLine($"## {CharacterData.GetCharacterName(cow)}");
                foreach (VoteButton btn in kvp.Value)
                {
                    string btnName = btn.GetComponentInChildren<Text>().text;
                    // if btn text doesnt work
                    // if (GameDescriptions.AlternateLocLookUp.ContainsKey(btn.m_Option.ToString())) btnName = GameDescriptions.AlternateLocLookUp[btn.m_Option.ToString()];
                    sb.AppendLine($"### {btnName} ({GameDescriptions.VoteOptionDescriptions[btn.m_Option]})"); // alternate
                    string slotResults = CombatUtils.GetDungeonSlotLegend(cow, btn);
                    if (slotResults.Length == 0) continue;
                    sb.AppendLine($"{slotResults}");
                    //expected => ### Cow #### Disarm (desc) - 0(2%) = Failure
                }
            }
            string encounterMsg = StaticMessage.Message;
            if (encounterMsg.Length != 0)
            {
                sb.Insert(0, $"## encountered {StaticMessage.Message}\n");
            }
            return sb.ToString().TrimEnd(['\n']);
        }

        // public static void AddItemUse(bool value)
        // {
        //     addItemUse = value;
        // }
    }
}