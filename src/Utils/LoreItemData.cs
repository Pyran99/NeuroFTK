using System.Collections.Generic;
using FTKItemName;
using Google2u;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public class LoreItemData
    {
        public static Dictionary<string, string> GetItemIdAndDescription(FTK_loreItem item)
        {
            Dictionary<string, string> data = [];
            string id = GetItemName(item);
            string description = GetItemDescription(item);
            data.Add(id, description);
            return data;
        }

        public static string GetItemName(FTK_loreItem item)
        {
            string id = "";
            switch (item.m_Category)
            {
                case FTK_loreCategory.ID.classes:
                    FTK_playerGameStart entry = FTK_playerGameStartDB.GetDB().GetEntry((FTK_playerGameStart.ID)item.m_UnlockID);
                    id = entry.GetDisplayName();
                    break;
                case FTK_loreCategory.ID.items:
                    // handled elsewhere
                    break;
                case FTK_loreCategory.ID.miniencounters:
                    FTK_miniEncounter entry2 = FTK_miniEncounterDB.GetDB().GetEntry((FTK_miniEncounter.ID)item.m_UnlockID);
                    id = entry2.GetDisplayName();
                    break;
                case FTK_loreCategory.ID.realms:
                    // probably not implemented?
                    // FTK_realm entry3 = FTK_realmDB.GetDB().GetEntry((FTK_realm.ID)item.m_UnlockID);
                    break;
                case FTK_loreCategory.ID.pois:
                    FTK_lorePois entry4 = FTK_lorePoisDB.GetDB().GetEntry((FTK_lorePois.ID)item.m_UnlockID);
                    switch (entry4.m_POIType)
                    {
                        case FTK_lorePois.POIType.Utility:
                        {
                            FTK_utility.ID unlockID = (FTK_utility.ID)entry4.m_UnlockID;
                            id = FTKHub.Localized<TextLore>("STR_Utility" + unlockID + "Display");
                            break;
                        }
                        case FTK_lorePois.POIType.POIs:
                        {
                            MiniHexInfo.MiniHexType unlockID2 = (MiniHexInfo.MiniHexType)entry4.m_UnlockID;
                            id = FTKHub.Localized<TextLore>("STR_" + unlockID2 + "Display");
                            break;
                        }
                        case FTK_lorePois.POIType.MiniEncounter:
                        {
                            FTK_miniEncounter entry5 = FTK_miniEncounterDB.GetDB().GetEntry((FTK_miniEncounter.ID)entry4.m_UnlockID);
                            id = entry5.GetDisplayName();
                            break;
                        }
                        case FTK_lorePois.POIType.StoneHero:
                        {
                            MiniHexStoneHero.StoneHeroType unlockID3 = (MiniHexStoneHero.StoneHeroType)entry4.m_UnlockID;
                            id = FTKHub.Localized<TextLore>("STR_" + unlockID3 + "Display");
                            break;
                        }
                        case FTK_lorePois.POIType.Sanctum:
                        {
                            FTK_sanctumStats.ID unlockID4 = (FTK_sanctumStats.ID)entry4.m_UnlockID;
                            id = FTKHub.Localized<TextLore>("STR_" + unlockID4 + "Display");
                            id = string.Format(FTKHub.Localized<TextMisc>("STR_sanctumGrand"), id);
                            break;
                        }
                    }
                    break;
                case FTK_loreCategory.ID.extraSkin:
                case FTK_loreCategory.ID.extraBackpack:
                case FTK_loreCategory.ID.extraHelmet:
                case FTK_loreCategory.ID.extraArmor:
                    FTK_loreExtraUnlock.ID unlockID5 = (FTK_loreExtraUnlock.ID)item.m_UnlockID;
                    FTK_loreExtraUnlock ftk_loreExtraUnlock = FTK_loreExtraUnlockDB.Get(unlockID5);
                    switch (ftk_loreExtraUnlock.m_ExtraType)
                    {
                        case FTK_loreExtraUnlock.ExtraType.Skin:
                            id = FTK_playerGameStart.GetSkinTypeDisplayName((FTK_playerGameStart.SkinType)ftk_loreExtraUnlock.m_UnlockID);
                            break;
                        case FTK_loreExtraUnlock.ExtraType.BackPack:
                            id = FTK_customizeBackpackDB.Get((FTK_customizeBackpack.ID)ftk_loreExtraUnlock.m_UnlockID).m_DisplayName;
                            id = FTKHub.Localized<TextMenu>(id);
                            break;
                        case FTK_loreExtraUnlock.ExtraType.Helmet:
                            id = FTK_customizeHelmetDB.Get((FTK_customizeHelmet.ID)ftk_loreExtraUnlock.m_UnlockID).m_DisplayName;
                            id = FTKHub.Localized<TextMenu>(id);
                            break;
                        case FTK_loreExtraUnlock.ExtraType.Armor:
                            id = FTK_customizeArmorDB.Get((FTK_customizeArmor.ID)ftk_loreExtraUnlock.m_UnlockID).m_DisplayName;
                            id = FTKHub.Localized<TextMenu>(id);
                            break;
                    }
                    break;
                default:
                    id = "";
                    break;
            }
            return id;
        }

        public static string GetItemDescription(FTK_loreItem item)
        {
            if (item.m_Category == FTK_loreCategory.ID.items) return "";
            if (item.m_Category == FTK_loreCategory.ID.classes)
            {
                FTK_playerGameStart entry = FTK_playerGameStartDB.GetDB().GetEntry((FTK_playerGameStart.ID)item.m_UnlockID);
                return FTKHub.Localized<TextCharacters>(entry.m_Flavor);
            }
            return FTKHub.Localized<TextLoreStore>(item.m_CardDescription);
        }

        // {name: description}
        public static Dictionary<string, string> HandleEquipmentDetails(FTK_itembase.ID itemId)
        {
            Dictionary<string, string> data = [];
            FTK_itembase itemBase = FTK_itembase.GetItemBase(itemId);
            string trName = itemBase.GetLocalizedName();
            string trDescription;
            if (itemBase.m_ObjectType == FTK_itembase.ObjectType.weapon)
            {
                trDescription = GetWeaponDetails((FTK_weaponStats2)itemBase);
            }
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.armor || itemBase.m_ObjectType == FTK_itembase.ObjectType.shield || itemBase.m_ObjectType == FTK_itembase.ObjectType.helmet || itemBase.m_ObjectType == FTK_itembase.ObjectType.boots || itemBase.m_ObjectType == FTK_itembase.ObjectType.shield || itemBase.m_ObjectType == FTK_itembase.ObjectType.trinket || itemBase.m_ObjectType == FTK_itembase.ObjectType.necklace)
            {
                if (FTK_characterModifierDB.GetDB().IsContainID(itemBase.m_ID))
                {
                    trDescription = CharacterSkills.GetModDisplay(FTK_characterModifierDB.Get(itemBase.m_ID), false);
                }
                else
                {
                    Plugin.Logger.LogWarning("equipment database error");
                    trDescription = "";
                }
            }
            else if (FTK_itembase.IsPipeItem(itemId))
            {
                trDescription = FTK_pipeDB.GetDB().GetPipeEntryFromItem(itemId).GetItemCardDescription();
            }
            else
            {
                trDescription = FTKItem.Get(itemId)?.GetDescription(null);
            }
            data.Add(trName, trDescription);
            return data;
        }

        // returns that description/values of a weapon
        public static string GetWeaponDetails(FTK_weaponStats2 weaponStats)
        {
            string maxDmg = weaponStats._maxdmg.ToString();
            string dmgType;
            switch (weaponStats._dmgtype)
            {
                case FTK_weaponStats2.DamageType.physical:
                    dmgType = FTKHub.Localized<TextMisc>("STR_charModPhysicalDamage");
                    break;
                case FTK_weaponStats2.DamageType.magic:
                    dmgType = FTKHub.Localized<TextMisc>("STR_charModMagicDamage");
                    break;
                default:
                    dmgType = "";
                    Plugin.Logger.LogWarning($"unknown damage type: {weaponStats._dmgtype}");
                    break;
            }
            string text1 = "";
            string text2 = "";
            string targetType = "target type: ";
            string attackProficiency = "";
            List<FTK_proficiencyTable.ID> list = [.. uiWeaponDetail.GetWeaponProfIDs(weaponStats)];
            if (!weaponStats.m_NoRegularAttack)
            {
                list.Insert(0, FTK_proficiencyTable.ID.None);
            }
            for (int i = 0; i < list.Count; i++)
            {
                FTK_proficiencyTable ftk_proficiencyTable = null;
                if (list[i] == FTK_proficiencyTable.ID.None)
                {
                    text1 += weaponStats.GetAttackDisplay();
                }
                else
                {
                    text1 += FTK_proficiencyTableDB.GetDB().GetEntry(list[i]).GetLocalizedDisplayTitle();
                    ftk_proficiencyTable = FTK_proficiencyTableDB.Get(list[i]);
                }
                if (i < list.Count - 1)
                {
                    text1 += ", ";
                }
                if (ftk_proficiencyTable != null && !ftk_proficiencyTable.m_TargetFriendly)
                {
                    CharacterDummy.TargetType target = ftk_proficiencyTable.m_Target;
                    if (target != CharacterDummy.TargetType.Aoe)
                    {
                        if (target != CharacterDummy.TargetType.Splash)
                        {
                            if (target == CharacterDummy.TargetType.None)
                            {
                                targetType += " single target,";
                            }
                        }
                        else
                        {
                            targetType += " splash,";
                        }
                    }
                    else
                    {
                        targetType += " aoe,";
                    }
                    if (ftk_proficiencyTable.m_DmgMultiplier > 1f)
                    {
                        attackProficiency = " heavy attack,";
                    }
                    if (ftk_proficiencyTable.m_IgnoresArmor)
                    {
                        attackProficiency += " pierce armor,";
                    }
                }
            }
            if (FTK_characterModifierDB.GetDB().IsContainID(weaponStats.m_ID))
            {
                text2 = CharacterSkills.GetModDisplay(FTK_characterModifierDB.GetDB().GetEntryByStringID(weaponStats.m_ID), false);
            }
            // weapon damage: 20 physical damage; attacks and proficiencies: stab, shadow blades; modifiers: 5% crit chance, 8 speed
            string final = $"'damage:{maxDmg} {dmgType}' 'attacks: {text1}' 'modifiers: {text2}'";
            return final;
        }
        
    }
}