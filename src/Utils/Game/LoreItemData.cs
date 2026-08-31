using System.Collections.Generic;
using System.Text;
using Google2u;
using GridEditor;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.Utils
{
    public class LoreItemData
    {
        public static bool IsAvailable(FTK_loreItem item)
        {
            if (LoreStoreUnlocks.skipCustomization)
            {
                if (item.m_Category == FTK_loreCategory.ID.extraArmor || item.m_Category == FTK_loreCategory.ID.extraBackpack || item.m_Category == FTK_loreCategory.ID.extraHelmet || item.m_Category == FTK_loreCategory.ID.extraSkin) return false;
            }
            return item.IsRevealed() && !item.IsPurchased() && item.CanAfford();
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

        public static string FixName(string name)
        {
            return name switch
            {
                "HelmetMask01" => "HelmetBeastman",
                "HelmetMask02" => "HelmetOwlbear",
                "HelmetMask03" => "HelmetTriclops",
                _ => name,
            };
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

        /// <returns>## name: description (customize extra)</returns>
        public static string GetCategoryDescription(FTK_loreCategory category)
        {
            StringBuilder sb = new();
            string trName = FTKHub.Localized<TextMisc>(category.m_DisplayName);
            string trDescription = FTKHub.Localized<TextLoreStore>(category.m_CategoryDescription);
            string trExtra = "";
            switch (FTK_loreCategory.GetEnum(category.m_ID))
            {
                case FTK_loreCategory.ID.extraArmor:
                    trExtra = " (Tired of the same old tunic? Change the appearance of your character's starting armor with this special armor skin.)";
                    break;
                case FTK_loreCategory.ID.extraBackpack:
                    trExtra = " (Pack your bags! Change the appearance of any character's backpack with this special backpack skin.)";
                    break;
                case FTK_loreCategory.ID.extraHelmet:
                    trExtra = " (Adventure in style! Begin the game with a special default helmet, available for all characters.)";
                    break;
                case FTK_loreCategory.ID.extraSkin:
                    trExtra = " (Change the appearance of any character with this special character skin.)";
                    break;
                default:
                    break;
            }
            sb.Append($"## {trName}: {trDescription}{trExtra} ");
            return sb.ToString();
        }

        /// <param name="categoryData">each category & list of its cards</param>
        /// <param name="availableCards">new dictionary to add card names: card</param>
        public static string GetCardListContext(Dictionary<FTK_loreCategory, List<uiLoreCard>> categoryData, out Dictionary<string, uiLoreCard> availableCards)
        {
            availableCards = [];
            string cardName;
            string cardDesc;
            FTK_loreItem item;
            StringBuilder sb = new();
            foreach (KeyValuePair<FTK_loreCategory, List<uiLoreCard>> category in categoryData)
            {
                // ## name: description (customize extra)
                sb.AppendLine($"{GetCategoryDescription(category.Key)}");
                List<uiLoreCard> cards = category.Value;
                foreach (uiLoreCard card in cards)
                {
                    // - name: description
                    item = card.m_LoreItem;
                    if (item.m_Category != FTK_loreCategory.ID.items)
                    {
                        cardName = GetItemName(item);
                        if (item.GetItemIDType() == typeof(FTK_loreExtraUnlock.ID))
                        {
                            cardDesc = "";
                        }
                        else cardDesc = $"{GetItemDescription(item)}";
                    }
                    else
                    {
                        FTK_itembase.ID itemId = (FTK_itembase.ID)item.m_UnlockID;
                        cardName = ItemData.GetItemName(itemId);
                        cardDesc = $"{ItemData.GetItemDescription(itemId, null, true, true)}";
                    }
                    if (item.m_Category == FTK_loreCategory.ID.extraArmor || item.m_Category == FTK_loreCategory.ID.extraBackpack || item.m_Category == FTK_loreCategory.ID.extraHelmet || item.m_Category == FTK_loreCategory.ID.extraSkin)
                    {
                        cardName = FixName(item.m_ID);
                    }
                    if (availableCards.ContainsKey(cardName))
                    {
                        Plugin.Logger.LogError($"dupe key {cardName}");
                        continue;
                    }
                    sb.AppendLine($"- [{cardName}] {cardDesc}");
                    availableCards.Add(cardName, card);
                }
            }
            return sb.ToString();
        }
    }
}