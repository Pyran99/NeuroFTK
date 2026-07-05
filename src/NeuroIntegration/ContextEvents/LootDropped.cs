using System.Collections.Generic;
using FTKItemName;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
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
            Plugin.Logger.LogWarning("DisplayLootItem: " + _item);
            FTK_itembase.ID id = FTK_itembase.GetEnum(_item);
            FTK_itembase itemBase = FTK_itembase.GetItemBase(id);
            string name = itemBase.GetLocalizedName();
            string rarity = FTKHub.Localized<TextMisc>(FTK_itemRarityLevelDB.GetDB().GetEntry(itemBase.m_ItemRarity).m_Display);
            string description;
            string amount = "";
            bool hasAmount = false;
            if (___m_LootItem.Contains("_gold_") || ___m_LootItem.Contains("_lore_")) hasAmount = true;
            if (hasAmount && ___m_LootItemCount > 0) amount = $"(x{___m_LootItemCount})";
            if (itemBase.m_ObjectType == FTK_itembase.ObjectType.weapon)
            {
                FTK_weaponStats2 stats = (FTK_weaponStats2)itemBase;
                string dmg = stats._maxdmg.ToString();
                string dmgType = stats._dmgtype == FTK_weaponStats2.DamageType.physical ? FTKHub.Localized<TextMisc>("STR_charModPhysicalDamage") : FTKHub.Localized<TextMisc>("STR_charModMagicDamage");
                string hands = stats.m_ObjectSlot == FTK_itembase.ObjectSlot.twoHands ? "Two-Handed" : "One-Handed";
                string breakable = stats.m_CanBreak == true ? "Breaks on critical fail" : "";
                string profs = "";
                List<FTK_proficiencyTable.ID> list = [.. uiWeaponDetail.GetWeaponProfIDs(stats)];
                if (!stats.m_NoRegularAttack) list.Insert(0, FTK_proficiencyTable.ID.None);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == FTK_proficiencyTable.ID.None) profs += stats.GetAttackDisplay();
                    else
                    {
                        profs += FTK_proficiencyTableDB.GetDB().GetEntry(list[i]).GetLocalizedDisplayTitle();
                    }
                    if (i < list.Count - 1) profs += ", ";
                }
                description = $"{dmg} {dmgType}, {hands}, {breakable} [Abilities]{profs}";
            }
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.armor || itemBase.m_ObjectType == FTK_itembase.ObjectType.shield || itemBase.m_ObjectType == FTK_itembase.ObjectType.helmet || itemBase.m_ObjectType == FTK_itembase.ObjectType.boots || itemBase.m_ObjectType == FTK_itembase.ObjectType.shield || itemBase.m_ObjectType == FTK_itembase.ObjectType.trinket || itemBase.m_ObjectType == FTK_itembase.ObjectType.necklace)
            {
                description = CharacterSkills.GetModDisplay(FTK_characterModifierDB.Get(itemBase.m_ID), true);
            }
            else if (FTK_itembase.IsPipeItem(id))
            {
                description = FTK_pipeDB.GetDB().GetPipeEntryFromItem(id)?.GetItemCardDescription();
            }
            else
            {
                CharacterOverworld cow = GameLogic.Instance.GetCurrentCombatCOW() ?? GameLogic.Instance.GetCurrentCOW();
                if (cow == null)
                {
                    Plugin.Logger.LogError("null cow");
                    return;
                }
                description = FTKItem.Get(id)?.GetDescription(cow);
            }

            Context.Send($"[Loot] {name}{amount} [Rarity] {StringReplace.RemoveStyling(rarity)} [Description] {StringReplace.RemoveStyling(description)}");
            // [the loot item to decide on]Gold Coins [Rarity]Common [Description]Currency of Fahrul. Each coin worth its weight in gold.
        }
    }
}