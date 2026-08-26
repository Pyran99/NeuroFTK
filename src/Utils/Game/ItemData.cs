using System.Collections.Generic;
using FTKItemName;
using Google2u;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    /// <summary>
    /// get names & descriptions of FTK_itemBase items
    /// </summary>
    public class ItemData
    {
        /// <summary>
        /// items that arnt implemented or verified to work
        /// </summary>
        static readonly List<FTK_itembase.ID> blacklistItems =
        [
            // FTK_itembase.ID.scrollvision, // this allows picking any hex, does not create list
            // FTK_itembase.ID.scrollpurify,
            // FTK_itembase.ID.scrollteleport,
            // FTK_itembase.ID.scrollgroupteleport,
            // FTK_itembase.ID.scrollidentify,
            // FTK_itembase.ID.scrollportal,
            // FTK_itembase.ID.townTeleport,
        ];

        /// <returns>{name: description}</returns>
        public static Dictionary<string, string> HandleEquipmentDetails(FTK_itembase.ID itemId)
        {
            Dictionary<string, string> data = [];
            string trName = GetItemName(itemId);
            string trDescription = GetItemDescription(itemId, null);
            data.Add(trName, trDescription);
            return data;
        }

        public static string GetItemName(FTK_itembase.ID _id)
        {
            return FTKHub.Instance.GetItemDisplayName(_id);
        }

        public static string GetItemDescription(FTK_itembase.ID _id, CharacterOverworld _cow, bool removeStyling = true, bool replaceNewLine = false)
        {
            FTK_itembase itemBase = FTK_itembase.GetItemBase(_id);
            string result;
            if (itemBase.m_ObjectType == FTK_itembase.ObjectType.weapon) result = GetWeaponData(_id);
            else if (itemBase.m_Equippable) result = GetEquipmentData(_id);
            else if (FTK_itembase.IsPipeItem(_id)) result = GetPipeData(_id);
            else result = GetOtherData(_id, _cow);

            if (removeStyling) result = StringReplace.RemoveStyling(result);
            if (replaceNewLine) return StringReplace.ReplaceNewLine(result);
            return result;
        }

        public static List<FTK_itembase.ID> GetUsableBeltItems(CharacterOverworld _cow)
        {
            List<FTK_itembase.ID> list = [];
            foreach (FTK_itembase.ID item in _cow.m_CharacterStats.GetBeltItems())
            {
                if (IsBlacklistItem(item)) continue;
                if (!FTKItem.Get(item).CanUse(_cow)) continue;
                list.Add(item);
            }
            return list;
        }

        public static bool IsBlacklistItem(FTK_itembase.ID id)
        {
            if (blacklistItems.Contains(id))
            {
                Plugin.Logger.LogMessage("blacklist item " + id);
                return true;
            }
            return false;
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
            string result = $"{dmg} {dmgType}, {hands}, {breakable} (Abilities) {profs}";
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

        public static string GetOtherData(FTK_itembase.ID _id, CharacterOverworld _cow)
        {
            if (_cow == null)
            {
                Plugin.Logger.LogError("null cow");
                return "";
            }
            return FTKItem.Get(_id)?.GetDescription(_cow);
        }
    }
}