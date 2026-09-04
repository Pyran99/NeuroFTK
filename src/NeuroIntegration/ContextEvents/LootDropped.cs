using System.Text;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents
{
    [HarmonyPatch]
    public class LootDropped
    {
        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.DisplayLootItem))]
        [HarmonyPostfix]
        static void CtxDisplayedLootItem(string _item, int ___m_LootItemCount, string ___m_LootItem)
        {
            FTK_itembase.ID id = FTK_itembase.GetEnum(_item);
            if (id == FTK_itembase.ID.None && _item.Contains("_life_"))
            {
                Context.Send($"[Loot] Increase life pool");
                return;
            }
            FTK_itembase itemBase = FTK_itembase.GetItemBase(id);
            string name = itemBase.GetLocalizedName();
            string rarity = FTKHub.Localized<TextMisc>(FTK_itemRarityLevelDB.GetDB().GetEntry(itemBase.m_ItemRarity).m_Display);
            string description;
            string amount = "";
            bool hasAmount = false;
            if (___m_LootItem.Contains("_gold_") || ___m_LootItem.Contains("_lore_")) hasAmount = true;
            if (hasAmount && ___m_LootItemCount > 0) amount = $"(x{___m_LootItemCount})";
            description = ItemData.GetItemDescription(id, CharacterData.GetActiveCow(), true, true);
            string lootMsg = $"[Loot] {name}{amount} ({StringReplace.RemoveStyling(rarity)}): {description}. ";
            if (itemBase.m_Equippable)
            {
                lootMsg += "\n";
                foreach (CharacterDummy dummy in EncounterSession.Instance.m_PlayerDummies.Values)
                {
                    if (!dummy.m_CharacterOverworld) continue;
                    CharacterOverworld cow = dummy.m_CharacterOverworld;
                    lootMsg += GetEquipmentCtx(id, cow);
                }
                lootMsg += "armor/resistance/evasion is useful for any class.";
            }
            Context.Send(lootMsg);
            // [loot] Gold Coins (Common): Currency of Fahrul. Each coin worth its weight in gold.
            // neuro has helmet helm1: helm1 data
            // evil has helmet helm2: helm2 data
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.AddItemToBackpackRPC))]
        [HarmonyPostfix]
        static void ItemLooted(CharacterOverworld __instance, FTK_itembase.ID _item)
        {
            if (!GlobalConfig.gameInitialized) return;
            Context.Send($"{CharacterData.GetCharacterName(__instance)} looted {ItemData.GetItemName(_item)}");
        }

        static string GetEquipmentCtx(FTK_itembase.ID id, CharacterOverworld cow)
        {
            StringBuilder sb = new();
            // PlayerInventory.ContainerID container = PlayerInventory.ContainerID.Belt;
            FTK_itembase _item = FTK_itembase.GetItemBase(id);
            PlayerInventory.ContainerID container = CharacterData.GetContainerForItem(_item.m_ObjectType);
            if (container != PlayerInventory.ContainerID.Belt) sb.AppendLine(GetEquippedItemData(id, cow, container));
            return sb.ToString();
        }

        static string GetEquippedItemData(FTK_itembase.ID id, CharacterOverworld cow, PlayerInventory.ContainerID container)
        {
            StringBuilder sb = new();
            bool isWeapon = false;
            FTK_itembase lootItemBase = FTK_itembase.GetItemBase(id);
            FTK_itembase.ObjectType lootType = lootItemBase.m_ObjectType;
            FTK_itembase.ID equipped = FTK_itembase.ID.None;
            string name = CharacterData.GetCharacterName(cow);
            switch (lootType)
            {
                case FTK_itembase.ObjectType.weapon:
                case FTK_itembase.ObjectType.shield:
                    isWeapon = true;
                    break;
                default:
                    equipped = cow.m_PlayerInventory.Get(container).GetOne();
                    break;
            }
            if (isWeapon)
            {
                FTK_itembase.ID equippedWeapon = cow.m_PlayerInventory.Get(PlayerInventory.ContainerID.RightHand).GetOne();
                FTK_itembase.ID equippedShield = cow.m_PlayerInventory.Get(PlayerInventory.ContainerID.LeftHand).GetOne();
                if (lootType == FTK_itembase.ObjectType.shield)
                {
                    if (equippedShield != FTK_itembase.ID.None) // replace shield
                    {
                        sb.Append($"{name} has {ItemData.GetItemName(equippedShield)}: {ItemData.GetItemDescription(equippedShield, cow, true, true)}.");
                        sb.Append(StringMessages.CharacterEquipped.Format(name, ItemData.GetItemName(equippedShield), ItemData.GetItemDescription(equippedShield, cow, true, true)));
                    }
                    else if (equippedWeapon != FTK_itembase.ID.None)
                    {
                        // int equippedHands = FTK_itembase.GetItemBase(equippedWeapon).m_WeaponHands;
                        // if (equippedHands == 2) // shield replace 2hand
                        // {
                        //     sb.Append($"{name} has {ItemData.GetItemName(equippedWeapon)}: {ItemData.GetItemDescription(equippedWeapon, true, cow)} (equipping the loot will unequip this weapon)");
                        // }
                        // else // shield with 1hand only
                        // {
                            sb.Append($"{name} has no item in {container}.");
                        // }
                    }
                    else
                    {
                        sb.Append($"{name} has no weapon equipped.");
                    }
                }
                else if (lootType == FTK_itembase.ObjectType.weapon)
                {
                    if (equippedWeapon != FTK_itembase.ID.None)
                    {
                        sb.Append(StringMessages.CharacterEquipped.Format(name, ItemData.GetItemName(equippedWeapon), ItemData.GetItemDescription(equippedWeapon, cow, true, true)));
                    }
                    else
                    {
                        sb.Append($"{name} has no item in {container}.");
                    }
                }
            }
            else
            {
                if (equipped == FTK_itembase.ID.None)
                {
                    sb.Append($"{name} has no {lootType} equipped");
                }
                else
                {
                    sb.Append(StringMessages.CharacterEquipped.Format(name, ItemData.GetItemName(equipped), ItemData.GetItemDescription(equipped, cow, true, true)));
                }
            }
            sb.Append($" (they want equipment that uses or increases {CharacterData.GetClassMainStat(cow.m_CharacterStats.m_CharacterClass)} stats).");
            return sb.ToString();
        }
    }
}