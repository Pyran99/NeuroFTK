using System;
using System.Collections.Generic;
using System.Linq;
using FTKItemName;
using Google2u;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Newtonsoft.Json;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItems(uiLoreStore store, List<uiLoreCard> cards) : NeuroAction<string>
    {
        public uiLoreStore uiLoreStore = store;
        public List<uiLoreCard> uiLoreCards = cards;

        public override string Name => "lore store purchase items";

        protected override string Description => "NYI";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.String,
            Required = ["test"],
            Properties = new Dictionary<string, JsonSchema>()
            {
                ["test"] = QJS.Enum(GenerateSchema()),
                ["test2"] = new JsonSchema
                {
                    Enum = ["1", "2", "3"],
                }
            }
        };

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage("execute purchase lore items action");
            if (parsedData == "1")
            {
                Plugin.Logger.LogMessage("close store");
                uiLoreStore.OnClose();
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogMessage($"validate purchase lore items action: {actionData.Data}");
            parsedData = (string)actionData.Data;
            return ExecutionResult.Success();
        }

        List<string> GenerateSchema()
        {
            return [.. UnlockableLoreItems().Select(l => l.Key.ToLower())]; //TODO send all items data to neuro to instead of just keys
            // return [.. UnlockableLoreItems().Select(l => l.m_ID)];
        }

        // get every item that can be purchased
        Dictionary<string, string> UnlockableLoreItems()
        {
            Dictionary<string, string> allLoreData = GetAllItemsDetails(uiLoreCards);
            allLoreData.OrderBy(kvp => kvp.Key);
            string json = JsonConvert.SerializeObject(allLoreData, Formatting.Indented);
            Plugin.Logger.LogMessage("card title: item description\n" + json);
            // GetCategoryData();
            return allLoreData;
        }

        Dictionary<string, string> GetAllItemsDetails(List<uiLoreCard> cards)
        {
            Dictionary<string, string> allLoreData = [];
            Dictionary<string, string> entry = [];
            FTK_loreItem item;
            foreach (uiLoreCard card in uiLoreCards)
            {
                item = card.m_LoreItem;
                if (!item.IsRevealed()) continue;
                Plugin.Logger.LogMessage(item.m_ID + " " + item.m_UnlockID);
                // if (item.IsPurchased()) continue;
                // if (!item.CanAfford()) continue;
                if (item.m_Category != FTK_loreCategory.ID.items)
                {
                    // ShowOtherLoreItem
                    entry = GetItemIdAndDescription(item);
                }
                else
                {
                    // this.m_ItemDetail.Show(_itemID, uiItemDetail.Mode.ItemDisplay, _cow, false, _forceFrontSide, _loreCard);
                    FTK_itembase itemBase = FTK_itembase.GetItemBase((FTK_itembase.ID)item.m_UnlockID);
                    string trName = itemBase.GetLocalizedName();
                    // if type is weapon, elif type is armors, elif pipe, else
                    entry = HandleEquipmentDetails((FTK_itembase.ID)item.m_UnlockID);
                    // replace below

                    // if (FTK_itembase.IsPipeItem((FTK_itembase.ID)item.m_UnlockID)) 
                    // {
                    //     Plugin.Logger.LogWarning("pipe item not implemented");
                    //     continue;
                    // }
                    // FTKItem ftkItem = FTKItem.Get((FTK_itembase.ID)item.m_UnlockID);
                    // if (allLoreData.ContainsKey(trName))
                    // {
                    //     Plugin.Logger.LogWarning($"duplicate item names {trName}");
                    //     continue;
                    // }
                    // allLoreData.Add(trName, ftkItem.GetDescription(null));
                    // continue;
                    //
                }
                if (allLoreData.ContainsKey(entry.Keys?.First())) continue;
                allLoreData.Add(entry.Keys?.First(), entry.Values?.First());
            }
            return allLoreData;
        }

        Dictionary<string, string> GetItemIdAndDescription(FTK_loreItem item)
        {
            Dictionary<string, string> data = [];
            string id = "";
            string description = TextLoreStore.Instance.Rows[(int)Enum.Parse(typeof(TextLoreStore.rowIds), item.m_CardDescription)]?.GetStringDataByIndex(0);
            switch (item.m_Category)
            {
                // manual translate => TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), category.m_DisplayName)];
                // not usable translate => FTKHub.Localized<TextCharacters>(entry.m_Flavor);
                case FTK_loreCategory.ID.classes:
                    FTK_playerGameStart entry = FTK_playerGameStartDB.GetDB().GetEntry((FTK_playerGameStart.ID)item.m_UnlockID);
                    id = entry.GetDisplayName();
                    description = TextCharacters.Instance.Rows[(int)Enum.Parse(typeof(TextCharacters.rowIds), entry.m_Flavor)].GetStringDataByIndex(0);
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
                    FTK_realm entry3 = FTK_realmDB.GetDB().GetEntry((FTK_realm.ID)item.m_UnlockID);
                    break;
                case FTK_loreCategory.ID.pois:
                    FTK_lorePois entry4 = FTK_lorePoisDB.GetDB().GetEntry((FTK_lorePois.ID)item.m_UnlockID);
                    switch (entry4.m_POIType)
                    {
                        case FTK_lorePois.POIType.Utility:
                        {
                            FTK_utility.ID unlockID = (FTK_utility.ID)entry4.m_UnlockID;
                            id = TextLore.Instance.Rows[(int)Enum.Parse(typeof(TextLore.rowIds), "STR_Utility" + unlockID + "Display")].GetStringDataByIndex(0);
                            // id = FTKHub.Localized<TextLore>("STR_Utility" + unlockID + "Display");
                            break;
                        }
                        case FTK_lorePois.POIType.POIs:
                        {
                            MiniHexInfo.MiniHexType unlockID2 = (MiniHexInfo.MiniHexType)entry4.m_UnlockID;
                            id = TextLore.Instance.Rows[(int)Enum.Parse(typeof(TextLore.rowIds), "STR_" + unlockID2 + "Display")].GetStringDataByIndex(0);
                            // id = FTKHub.Localized<TextLore>("STR_" + unlockID2 + "Display");
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
                            id = TextLore.Instance.Rows[(int)Enum.Parse(typeof(TextLore.rowIds), "STR_" + unlockID3 + "Display")].GetStringDataByIndex(0);
                            // id = FTKHub.Localized<TextLore>("STR_" + unlockID3 + "Display");
                            break;
                        }
                        case FTK_lorePois.POIType.Sanctum:
                        {
                            FTK_sanctumStats.ID unlockID4 = (FTK_sanctumStats.ID)entry4.m_UnlockID;
                            id = TextLore.Instance.Rows[(int)Enum.Parse(typeof(TextLore.rowIds), "STR_" + unlockID4 + "Display")].GetStringDataByIndex(0);
                            id = string.Format(TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), "STR_sanctumGrand")].GetStringDataByIndex(0), id);
                            // id = FTKHub.Localized<TextLore>("STR_" + unlockID4 + "Display");
                            // id = string.Format(FTKHub.Localized<TextMisc>("STR_sanctumGrand"), id);
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
                            id = TextMenu.Instance.Rows[(int)Enum.Parse(typeof(TextMenu.rowIds), id)].GetStringDataByIndex(0);
                            // id = FTKHub.Localized<TextMenu>(id);
                            break;
                        case FTK_loreExtraUnlock.ExtraType.Helmet:
                            id = FTK_customizeHelmetDB.Get((FTK_customizeHelmet.ID)ftk_loreExtraUnlock.m_UnlockID).m_DisplayName;
                            id = TextMenu.Instance.Rows[(int)Enum.Parse(typeof(TextMenu.rowIds), id)].GetStringDataByIndex(0);
                            // id = FTKHub.Localized<TextMenu>(id);
                            break;
                        case FTK_loreExtraUnlock.ExtraType.Armor:
                            id = FTK_customizeArmorDB.Get((FTK_customizeArmor.ID)ftk_loreExtraUnlock.m_UnlockID).m_DisplayName;
                            id = TextMenu.Instance.Rows[(int)Enum.Parse(typeof(TextMenu.rowIds), id)].GetStringDataByIndex(0);
                            // id = FTKHub.Localized<TextMenu>(id);
                            break;
                    }
                    break;
                
                default:
                    id = "";
                    description = "";
                    break;
            }
            
            data.Add(id, description);
            return data;
        }

        Dictionary<string, string> HandleEquipmentDetails(FTK_itembase.ID itemId)
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
            trDescription.Replace("\\n", ", ");
            // trDescription.Replace("\n", ", ");
            Plugin.Logger.LogMessage($"trName: {trName}, trDescription: {trDescription}");
            data.Add(trName, trDescription);
            return data;
        }

        // returns that values of a weapon
        string GetWeaponDetails(FTK_weaponStats2 weaponStats)
        {
            string maxDmg = weaponStats._maxdmg.ToString();
            string dmgType;
            switch (weaponStats._dmgtype)
            {
                case FTK_weaponStats2.DamageType.physical:
                    dmgType = TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), "STR_charModPhysicalDamage")].GetStringDataByIndex(0);
                    break;
                case FTK_weaponStats2.DamageType.magic:
                    dmgType = TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), "STR_charModMagicDamage")].GetStringDataByIndex(0);
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
                                // this.m_SingleTargetIcon.SetActive(true);
                            }
                        }
                        else
                        {
                            targetType += " splash,";
                            // this.m_SplashIcon.SetActive(true);
                        }
                    }
                    else
                    {
                        targetType += " aoe,";
                        // this.m_AoeIcon.SetActive(true);
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
                text2.Replace("\n", ", ");
            }
            // weapon damage: 20 physical damage; attacks and proficiencies: stab, shadow blades; modifiers: 5% crit chance, 8 speed
            string final = $"weapon damage:{maxDmg} {dmgType}; attacks and proficiencies: {text1}; modifiers: {text2}";
            return final;
        }

    }
}

#region WEAPON DETAILS
// string text = string.Empty;
// string text2 = string.Empty;
// List<FTK_proficiencyTable.ID> list = new List<FTK_proficiencyTable.ID>(uiWeaponDetail.GetWeaponProfIDs(ftk_weaponStats));
// if (!ftk_weaponStats.m_NoRegularAttack)
// {
//     list.Insert(0, FTK_proficiencyTable.ID.None);
// }
// bool flag = false;
// bool flag2 = false;
// for (int i = 0; i < list.Count; i++)
// {
//     FTK_proficiencyTable ftk_proficiencyTable = null;
//     if (list[i] == FTK_proficiencyTable.ID.None)
//     {
//         text += ftk_weaponStats.GetAttackDisplay();
//     }
//     else
//     {
//         text += FTK_proficiencyTableDB.GetDB().GetEntry(list[i]).GetLocalizedDisplayTitle();
//         ftk_proficiencyTable = FTK_proficiencyTableDB.Get(list[i]);
//     }
//     if (i < list.Count - 1)
//     {
//         text += ", ";
//     }
//     if (ftk_proficiencyTable != null && !ftk_proficiencyTable.m_TargetFriendly)
//     {
//         CharacterDummy.TargetType target = ftk_proficiencyTable.m_Target;
//         if (target != CharacterDummy.TargetType.Aoe)
//         {
//             if (target != CharacterDummy.TargetType.Splash)
//             {
//                 if (target == CharacterDummy.TargetType.None)
//                 {
//                     this.m_SingleTargetIcon.SetActive(true);
//                 }
//             }
//             else
//             {
//                 this.m_SplashIcon.SetActive(true);
//             }
//         }
//         else
//         {
//             this.m_AoeIcon.SetActive(true);
//         }
//         if (ftk_proficiencyTable.m_DmgMultiplier > 1f)
//         {
//             flag = true;
//         }
//         if (ftk_proficiencyTable.m_IgnoresArmor)
//         {
//             flag2 = true;
//         }
//     }
// }
#endregion
