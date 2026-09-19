using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.NeuroIntegration.ContextEvents;
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
            if (Multiplayer.OtherPlayersAction(cow)) return;
            activeContainers.Add(__instance);
            string name = CharacterData.GetCharacterName(cow);
            voteButtons[cow] = [];
            VoteButton[] btns = __instance.GetComponentsInChildren<VoteButton>();
            foreach (VoteButton btn in btns)
            {
                if (btn != null) voteButtons[cow].Add(btn);
            }
            if (voteButtons[cow].Count == 0) voteButtons.Remove(cow);
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
            voteButtons.Remove(__instance.m_PlayerHud.m_Cow);
            activeContainers.Remove(__instance);
            if (GameLogic.Instance.m_GameMode == GameLogic.GameMode.Multiplayer)
            {
                if (voteButtons.Count > 0 && activeContainers.Count > 0)
                {
                    activeContainers.First().StartCoroutine(QuickTimerCallback.WaitRoutine(CreateAction, activeContainers.First().gameObject));
                    return;
                }
            }
            if (activeContainers.Count > 0) return;
            ResetData();
        }

        [HarmonyPatch(typeof(EncounterSessionMC), nameof(EncounterSessionMC.VoteButtonClick))]
        [HarmonyPostfix]
        static void Temp(VoteButton.VoteOption _voteOption)
        {
            switch (_voteOption)
            {
                case VoteButton.VoteOption.Pass:
                    if (!Multiplayer.IsMultiplayer()) LootDropped.lootMsg = string.Empty;
                    break;
                default:
                    LootDropped.lootMsg = string.Empty;
                    break;
            }
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
            bool lootDecision = false;
            int dupe = 0;
            List<string> usedNames = [];
            string name;
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                if (kvp.Value.Count == 0) Plugin.Logger.LogError($"no valid btns for {kvp.Key.m_PlayerName}");
                if (kvp.Value.Any(btn => ItemData.IsLootDecision(btn.m_Option))) lootDecision = true;
                name = CharacterData.GetCharacterName(kvp.Key);
                while (usedNames.Contains(name)) name = $"{name}{dupe++}";
                usedNames.Add(name);
                activeWindow.AddAction(new CharacterDecisionAction(kvp.Key, name, kvp.Value));
                sbState.AppendLine($"{CharacterData.GetDataFor(kvp.Key)} ");
            }
            sbState.Append($"{StringMessages.FocusDetails}");
            string query = Multiplayer.IsMultiplayer() ? StringMessages.DecisionButtonsPromptMultiplayer.Format(instance.m_Prompt.text) : StringMessages.DecisionButtonsPrompt.Format(instance.m_Prompt.text);
            if (lootDecision && Multiplayer.IsMultiplayer())
            {
                if (LootDropped.lootMsg != string.Empty) sbState.Append($" loot to decide on: {LootDropped.lootMsg}.");
            }
            activeWindow.SetForce(0, query, sbState.ToString(), true);
            StringBuilder sb = new(DungeonEncounterRolls());
            EncounterData encounter = EncounterSessionMC.Instance.GetCurrentEncounter();
            if (encounter != null)
            {
                MiniHexDungeon.EncounterType _encounterType = encounter.EncounterType;
                if (_encounterType == MiniHexDungeon.EncounterType.Next || _encounterType == MiniHexDungeon.EncounterType.Ready || _encounterType == MiniHexDungeon.EncounterType.Stair || _encounterType == MiniHexDungeon.EncounterType.EmptyRoom)
                {
                    // Plugin.Logger.LogWarning($"change equipment in dungeon check {_encounterType}");
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
                            string _name = CharacterData.GetCharacterName(dummy.m_CharacterOverworld);
                            sb2.AppendLine($"## {_name} has empty equipment slots, these items can be equipped to them. ");
                            sb2.AppendLine(equipSb.ToString());
                            sb2.AppendLine($"{_name} prefers {CharacterData.GetClassMainStat(dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterClass)} stats, avoid equipping items that reduce them (if 'any' you can choose what stats to avoid).");
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
                // foreach (CharacterDummy dummy in EncounterSession.Instance.m_PlayerDummies.Values)
                // {
                //     if (!dummy.m_CharacterOverworld) continue;
                //     if (!dummy.m_IsAlive) continue;
                //     // List<FTK_itembase.ID> items = ItemData.GetUsableBeltItems(dummy.m_CharacterOverworld);
                //     // Dictionary<string, FTK_itembase.ID> items2 = items.ToDictionary(ItemData.GetItemName, x => x);
                //     // if (items.Count > 0) activeWindow.AddAction(new UseBeltItemAction(items2, dummy.m_CharacterOverworld));
                // }
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

        public static void NeuroTryDecisionBtn(VoteButton btn, CharacterOverworld cow, int focusUsed)
        {
            isShowing = false;
            if (btn.m_Option == VoteButton.VoteOption.Ready)
            {
                if (GameLogic.Instance.IsMultiplayer())
                {
                    cow.StartCoroutine(CoroutineAllReadyPress());
                    return;
                }
            }
            if (focusUsed <= 0)
            {
                SelectButton.StartCoroutine(btn);
                return;
            }
            FTK_slotOutput.ID id = FTK_slotOutput.ID.None;
            if (EncounterSession.Instance.m_ActiveDiorama is DioramaDungeon dioramaDungeon)
            {
                switch (btn.m_Option)
                {
                    case VoteButton.VoteOption.Knockdown:
                        id = dioramaDungeon.m_DoorToBash.GetComponent<DungeonDoor>().GetDoorBashOutput(btn.m_Hud.m_Cow);
                        break;
                    case VoteButton.VoteOption.Disarm:
                        id = dioramaDungeon.m_ActiveTrap.GetDisarmOutput(btn.m_Hud.m_Cow);
                        break;
                    case VoteButton.VoteOption.Proceed:
                        id = dioramaDungeon.m_ActiveTrap.GetProceedOutput(btn.m_Hud.m_Cow);
                        break;
                    case VoteButton.VoteOption.Attempt:
                        id = dioramaDungeon.m_DungeonEncounter.m_EncounterObject.GetDBEntry().m_SlotRoll;
                        break;
                }
            }
            if (id == FTK_slotOutput.ID.None)
            {
                SelectButton.StartCoroutine(btn);
                return;
            }
            FTK_slotOutput entry = FTK_slotOutputDB.GetDB().GetEntry(id);
            if (!entry.m_CanFocus)
            {
                SelectButton.StartCoroutine(btn, 1.0f);
                return;
            }
            if (CharacterData.CanFocusAction(cow.m_CharacterStats, entry.m_SlotAmount, focusUsed))
            {
                SelectButton.StartCoroutineWithFocus(btn, focusUsed, cow.m_CharacterStats);
            }
            else SelectButton.StartCoroutine(btn, 1.0f);
        }

        static IEnumerator CoroutineAllReadyPress()
        {
            Dictionary<CharacterOverworld, List<VoteButton>> copy = new(voteButtons);
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in copy)
            {
                foreach (VoteButton btn in kvp.Value)
                {
                    if (btn.m_Option != VoteButton.VoteOption.Ready) continue;
                    btn.OnPointerEnter(null);
                    btn.OnControllerClick();
                    break;
                }
                yield return null;
            }
            yield break;
        }

        // public static void AddItemUse(bool value)
        // {
        //     addItemUse = value;
        // }
    }
}