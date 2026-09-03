using System.Collections;
using System.Collections.Generic;
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

        public static IEnumerator EquipItemsRoutine(List<FTK_itembase.ID> items, CharacterOverworld cow)
        {
            if (items.Count == 0)
            {
                Context.Send("you sent no items to equip", true);
                yield return new WaitForSeconds(1f);
                OverworldFlow.BeginTurn2(cow);
                yield break;
            }
            StringBuilder sb = new($"{CharacterData.GetCharacterName(cow)} equipped: ");
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
                yield return new WaitForSeconds(0.25f);
            }
            Context.Send(sb.ToString().TrimEnd([' ', ',']));
            Plugin.Logger.LogMessage("finished equipping items");
            yield return new WaitForSeconds(1f);
            OverworldFlow.BeginTurn2(cow);
        }
    }
}