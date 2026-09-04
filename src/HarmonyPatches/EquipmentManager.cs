using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public class EquipmentManager
    {
        // static void Test1(CharacterOverworld cow)
        // {
        //     PlayerInventory inv = cow.m_PlayerInventory;
        //     ItemContainer backpack = inv.m_ContainerBackpack;
        //     bool helmet = inv.m_ContainerHead.IsEmpty();
        // }

        public static PlayerInventory.ContainerID[] GetEquipmentContainers()
        {
            PlayerInventory.ContainerID[] containers =
            [
                PlayerInventory.ContainerID.LeftHand,
                PlayerInventory.ContainerID.RightHand,
                PlayerInventory.ContainerID.Head,
                PlayerInventory.ContainerID.Body,
                PlayerInventory.ContainerID.Foot,
                PlayerInventory.ContainerID.Neck,
                PlayerInventory.ContainerID.Trinket,
            ];
            return containers;
        }

        public static Dictionary<PlayerInventory.ContainerID, List<FTK_itembase.ID>> GetEquippableItems(CharacterOverworld cow, List<PlayerInventory.ContainerID> containers, out string context)
        {
            StringBuilder equipSb = new();
            Dictionary<PlayerInventory.ContainerID, List<FTK_itembase.ID>> result = [];
            if (containers.Count > 0)
            {
                foreach (PlayerInventory.ContainerID container in containers)
                {
                    List<FTK_itembase.ID> backpackItems = CharacterData.GetItemsForContainer(cow.m_PlayerInventory, container);
                    if (backpackItems.Count == 0 || result.ContainsKey(container)) continue;
                    equipSb.AppendLine($"### {container}");
                    result.Add(container, []);
                    foreach (FTK_itembase.ID item in backpackItems)
                    {
                        if (result[container].Contains(item)) continue;
                        equipSb.AppendLine($"- {ItemData.GetItemName(item)}: {ItemData.GetItemDescription(item, cow, true, true)}.");
                        result[container].Add(item);
                    }
                }
            }
            context = equipSb.ToString();
            return result;
        }

        /// <returns>container: {name: itemId}</returns>
        public static Dictionary<PlayerInventory.ContainerID, Dictionary<string, FTK_itembase.ID>> GetEquipDictionary(Dictionary<PlayerInventory.ContainerID, List<FTK_itembase.ID>> equippableItems)
        {
            Dictionary<PlayerInventory.ContainerID, Dictionary<string, FTK_itembase.ID>> result = [];
            foreach (PlayerInventory.ContainerID container in equippableItems.Keys)
            {
                result.Add(container, equippableItems[container].ToDictionary(x => ItemData.GetItemName(x, true), x => x));
            }
            return result;
        }
            

        public static IEnumerator EquipItemsRoutine(List<FTK_itembase.ID> items, CharacterOverworld cow)
        {
            if (items.Count == 0)
            {
                Context.Send("you sent no items to equip", true);
                yield return new WaitForSeconds(1f);
                ResetTurn(cow);
                yield break;
            }
            StringBuilder sb = new($"{CharacterData.GetCharacterName(cow)} equipped: ");
            cow.m_UIPlayMainHud.m_OpenInventory.OnSubmit(null);
            yield return new WaitForSeconds(0.5f);
            foreach (FTK_itembase.ID item in items)
            {
                if (item == FTK_itembase.ID.None) continue;
                FTK_itembase itemBase = FTK_itembase.GetItemBase(item);
                if (!itemBase.m_Equippable)
                {
                    Plugin.Logger.LogError("non equippable item passed in action");
                    continue;
                }
                if (PlayerInventory.CanForceEquip(item)) cow.ForceEquip(item);
                else cow.EquipItem(item);
                sb.Append($"{ItemData.GetItemName(item)}, ");
                yield return new WaitForSeconds(0.5f);
            }
            Context.Send(sb.ToString().TrimEnd([' ', ',']));
            Plugin.Logger.LogMessage("finished equipping items");
            yield return new WaitForSeconds(2f);
            uiPlayerInventory.Instance.OnClose();
            ResetTurn(cow);
        }

        static void ResetTurn(CharacterOverworld cow)
        {
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogWarning("overworld equip");
                OverworldFlow.BeginTurn2(cow);
            }
            else if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Dungeon)
            {
                Plugin.Logger.LogWarning("combat equip");
                List<VoteButtonContainer> containers = [];
                foreach (CharacterDummy dummy in EncounterSession.Instance.m_PlayerDummies.Values)
                {
                    if (!dummy.m_IsAlive) continue; // does dead dummy have votes
                    uiPlayerMainHud hud = dummy.m_CharacterOverworld.m_UIPlayMainHud;
                    VoteButtonContainer votes = hud.m_LootCollectionButtons;
                    votes.Hide();
                    containers.Add(votes);
                }
                foreach (VoteButtonContainer votes in containers)
                {
                    votes.Show(EncounterSessionMC.VoteType.Ready);
                }
            }
        }
    }
}