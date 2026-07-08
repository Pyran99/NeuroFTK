using System.Collections.Generic;
using FTKItemName;
using Google2u;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    /// <summary>
    /// NYI this could be used when needing to get item data in different scripts
    /// </summary>
    public class ItemData
    {
        public static Dictionary<string, string> GetAllItemData(List<FTK_itembase.ID> _ids)
        {
            Dictionary<string, string> data = [];
            foreach (FTK_itembase.ID _id in _ids)
            {
                data.Merge(GetItemData(_id));
            }
            return data;
        }

        public static Dictionary<string, string> GetItemData(FTK_itembase.ID _id)
        {
            Dictionary<string, string> data = [];
            string id = GetItemName(_id);
            string description = GetItemDescription(_id);
            data[id] = description;
            return data;
        }

        public static string GetItemName(FTK_itembase.ID _id)
        {
            return "";
        }

        public static string GetItemDescription(FTK_itembase.ID _id, bool removeStyling = true, CharacterOverworld _cow = null)
        {
            FTK_itembase itemBase = FTK_itembase.GetItemBase(_id);
            string result;
            if (itemBase.m_ObjectType == FTK_itembase.ObjectType.weapon) result = GetWeaponData(_id);
            else if (IsEquipmentType(itemBase.m_ObjectType)) result = GetEquipmentData(_id);
            else if (FTK_itembase.IsPipeItem(_id)) result = GetPipeData(_id);
            else
            {
                result = FTKItem.Get(_id)?.GetDescription(_cow);
            }
            if (removeStyling) result = StringReplace.RemoveStyling(result);
            return result;
        }

        public static bool IsEquipmentType(FTK_itembase.ObjectType type)
        {
            return type switch
            {
                FTK_itembase.ObjectType.armor or FTK_itembase.ObjectType.shield or FTK_itembase.ObjectType.helmet or FTK_itembase.ObjectType.boots or FTK_itembase.ObjectType.trinket or FTK_itembase.ObjectType.necklace => true,
                _ => false,
            };
        }

        /// <summary>
        /// return has styling
        /// </summary>
        /// <param name="_id"></param>
        /// <returns>$"{dmg} {dmgType}, {hands}, {breakable} [Abilities] {profs}";</returns>
        public static string GetWeaponData(FTK_itembase.ID _id)
        {
            FTK_itembase itemBase = FTK_itembase.GetItemBase(_id);
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
            string result = $"{dmg} {dmgType}, {hands}, {breakable} [Abilities] {profs}.";
            return result;
        }

        public static string GetEquipmentData(FTK_itembase.ID _id)
        {
            FTK_itembase itemBase = FTK_itembase.GetItemBase(_id);
            string result = CharacterSkills.GetModDisplay(FTK_characterModifierDB.Get(itemBase.m_ID), false);
            return result;
        }

        public static string GetPipeData(FTK_itembase.ID _id)
        {
            string result = FTK_pipeDB.GetDB().GetPipeEntryFromItem(_id)?.GetItemCardDescription();
            return result;
        }

        public static string GetOtherData(FTK_itembase.ID _id)
        {
            string result = "";
            return result;
        }
    }
}