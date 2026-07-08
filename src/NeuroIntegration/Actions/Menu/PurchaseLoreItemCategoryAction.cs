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
using Pyran.NeuroFTK.Utils;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    /// <summary>
    /// This is a test for making each lore category its own action. the descriptions could then be based on the category 
    /// NOT updated with current store action PurchaseLoreItemAction
    /// </summary>
    public class PurchaseLoreItemCategoryAction(uiLoreStore store, List<uiLoreCard> cards, string category, FTK_loreCategory.ID id) : NeuroAction<string>
    {
        public uiLoreStore uiLoreStore = store;
        public List<uiLoreCard> uiLoreCards = cards;
        public Action<PurchaseLoreItemCategoryAction> ItemPurchased;
        FTK_loreCategory.ID categoryID = id;
        static bool isPurchasing = false;


        public override string Name => $"purchase_{category.ToLower().Replace(" ", "_")}_item";

        protected override string Description => "purchase an item from the store. these unlock various things that can appear in future runs.";

        protected override JsonSchema Schema => GenerateSchema();

        protected override void Execute(string parsedData)
        {
            if (isPurchasing) return;
            isPurchasing = true;
            Plugin.Logger.LogMessage("execute purchase item action");
            isPurchasing = false;
            ItemPurchased.Invoke(this);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogMessage($"{Name} desired item to purchase: {actionData.Data}");
            parsedData = (string)actionData.Data;
            NeuroActionHandler.UnregisterActions(this);
            return ExecutionResult.Success();
        }

        private JsonSchema GenerateSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.String,
                Required = ["item"],
                Properties = new Dictionary<string, JsonSchema>
                {
                    ["item"] = QJS.Enum(UnlockableLoreItems().Keys)
                }
            };
            return schema;
        }

        // get every item that can be purchased
        Dictionary<string, string> UnlockableLoreItems()
        {
            Dictionary<string, string> allLoreData = GetAllItemsDetails(uiLoreCards);
            allLoreData.OrderByDescending(kvp => kvp.Key);
            // string json = JsonConvert.SerializeObject(allLoreData, Formatting.Indented);
            // Plugin.Logger.LogMessage("card title: item description\n" + json);
            return allLoreData;
        }

        Dictionary<string, string> GetAllItemsDetails(List<uiLoreCard> cards)
        {
            Dictionary<string, string> allLoreData = [];
            Dictionary<string, string> entry;
            FTK_loreItem item;
            foreach (uiLoreCard card in uiLoreCards)
            {
                item = card.m_LoreItem;
                if (item.m_Category != categoryID) continue;
                if (!item.IsRevealed()) continue;
                if (item.IsPurchased()) continue;
                if (!item.CanAfford()) continue;
                if (item.m_Category != FTK_loreCategory.ID.items)
                {
                    // ShowOtherLoreItem
                    entry = GetItemIdAndDescription(item);
                }
                else
                {
                    FTK_itembase itemBase = FTK_itembase.GetItemBase((FTK_itembase.ID)item.m_UnlockID);
                    entry = ItemData.HandleEquipmentDetails((FTK_itembase.ID)item.m_UnlockID);
                }
                if (allLoreData.ContainsKey(entry?.Keys?.First())) continue;
                allLoreData.Add(entry?.Keys?.First(), entry?.Values?.First());
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


        // // returns that values of a weapon
        // string GetWeaponDetails(FTK_weaponStats2 weaponStats)
        // {
        //     //TESTING
        //     return ItemData.GetItemDescription(FTK_itembase.GetEnum(weaponStats.m_ID));
        //     //
        //     // string maxDmg = weaponStats._maxdmg.ToString();
        //     // string dmgType;
        //     // switch (weaponStats._dmgtype)
        //     // {
        //     //     case FTK_weaponStats2.DamageType.physical:
        //     //         dmgType = TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), "STR_charModPhysicalDamage")].GetStringDataByIndex(0);
        //     //         break;
        //     //     case FTK_weaponStats2.DamageType.magic:
        //     //         dmgType = TextMisc.Instance.Rows[(int)Enum.Parse(typeof(TextMisc.rowIds), "STR_charModMagicDamage")].GetStringDataByIndex(0);
        //     //         break;
        //     //     default:
        //     //         dmgType = "";
        //     //         Plugin.Logger.LogWarning($"unknown damage type: {weaponStats._dmgtype}");
        //     //         break;
        //     // }
        //     // string text1 = "";
        //     // string text2 = "";
        //     // string targetType = "target type: ";
        //     // string attackProficiency = "";
        //     // List<FTK_proficiencyTable.ID> list = [.. uiWeaponDetail.GetWeaponProfIDs(weaponStats)];
        //     // if (!weaponStats.m_NoRegularAttack)
        //     // {
        //     //     list.Insert(0, FTK_proficiencyTable.ID.None);
        //     // }
        //     // for (int i = 0; i < list.Count; i++)
        //     // {
        //     //     FTK_proficiencyTable ftk_proficiencyTable = null;
        //     //     if (list[i] == FTK_proficiencyTable.ID.None)
        //     //     {
        //     //         text1 += weaponStats.GetAttackDisplay();
        //     //     }
        //     //     else
        //     //     {
        //     //         text1 += FTK_proficiencyTableDB.GetDB().GetEntry(list[i]).GetLocalizedDisplayTitle();
        //     //         ftk_proficiencyTable = FTK_proficiencyTableDB.Get(list[i]);
        //     //     }
        //     //     if (i < list.Count - 1)
        //     //     {
        //     //         text1 += ", ";
        //     //     }
        //     //     if (ftk_proficiencyTable != null && !ftk_proficiencyTable.m_TargetFriendly)
        //     //     {
        //     //         CharacterDummy.TargetType target = ftk_proficiencyTable.m_Target;
        //     //         if (target != CharacterDummy.TargetType.Aoe)
        //     //         {
        //     //             if (target != CharacterDummy.TargetType.Splash)
        //     //             {
        //     //                 if (target == CharacterDummy.TargetType.None)
        //     //                 {
        //     //                     targetType += " single target,";
        //     //                     // this.m_SingleTargetIcon.SetActive(true);
        //     //                 }
        //     //             }
        //     //             else
        //     //             {
        //     //                 targetType += " splash,";
        //     //                 // this.m_SplashIcon.SetActive(true);
        //     //             }
        //     //         }
        //     //         else
        //     //         {
        //     //             targetType += " aoe,";
        //     //             // this.m_AoeIcon.SetActive(true);
        //     //         }
        //     //         if (ftk_proficiencyTable.m_DmgMultiplier > 1f)
        //     //         {
        //     //             attackProficiency = " heavy attack,";
        //     //         }
        //     //         if (ftk_proficiencyTable.m_IgnoresArmor)
        //     //         {
        //     //             attackProficiency += " pierce armor,";
        //     //         }
        //     //     }
        //     // }
        //     // if (FTK_characterModifierDB.GetDB().IsContainID(weaponStats.m_ID))
        //     // {
        //     //     text2 = CharacterSkills.GetModDisplay(FTK_characterModifierDB.GetDB().GetEntryByStringID(weaponStats.m_ID), false);
        //     //     text2.Replace(@"\n", ", ");
        //     // }
        //     // // weapon damage: 20 physical damage; attacks and proficiencies: stab, shadow blades; modifiers: 5% crit chance, 8 speed
        //     // string final = $"weapon damage:{maxDmg} {dmgType}; attacks and proficiencies: {text1}; modifiers: {text2}";
        //     // return final;
        // }

        
    }
}

#region game code
/// this creates all the categories & card lists
// List<FTK_loreCategory> list = new List<FTK_loreCategory>(FTK_loreCategoryDB.GetDB().m_Array);
// 			list = list.OrderBy((FTK_loreCategory a) => a.m_SortPriority).ToList<FTK_loreCategory>();
// 			Dictionary<int, List<FTK_loreCategory>> dictionary = new Dictionary<int, List<FTK_loreCategory>>();
// 			foreach (FTK_loreCategory ftk_loreCategory in list)
// 			{
// 				if (!dictionary.ContainsKey(ftk_loreCategory.m_SortPriority))
// 				{
// 					dictionary[ftk_loreCategory.m_SortPriority] = new List<FTK_loreCategory>();
// 				}
// 				dictionary[ftk_loreCategory.m_SortPriority].Add(ftk_loreCategory);
// 			}
// 			Dictionary<FTK_loreCategory.ID, List<FTK_loreItem.ID>> dictionary2 = new Dictionary<FTK_loreCategory.ID, List<FTK_loreItem.ID>>();
// 			FTK_loreCategory.ID id = FTK_loreCategory.ID.None;
// 			foreach (List<FTK_loreCategory> list2 in dictionary.Values)
// 			{
// 				int num = 0;
// 				foreach (FTK_loreCategory ftk_loreCategory2 in list2)
// 				{
// 					Dictionary<string, List<FTK_loreItem>> dictionary3 = new Dictionary<string, List<FTK_loreItem>>
// 					{
// 						{
// 							FTK_statistic.ID.STAT_LU_PROGRESS_ACTI.ToString(),
// 							new List<FTK_loreItem>()
// 						},
// 						{
// 							FTK_statistic.ID.STAT_LU_PROGRESS_ACTII.ToString(),
// 							new List<FTK_loreItem>()
// 						},
// 						{
// 							FTK_statistic.ID.STAT_LU_PROGRESS_ACTIII.ToString(),
// 							new List<FTK_loreItem>()
// 						},
// 						{
// 							FTK_statistic.ID.STAT_LU_PROGRESS_ACTIV.ToString(),
// 							new List<FTK_loreItem>()
// 						}
// 					};
// 					List<FTK_loreItem> list3 = new List<FTK_loreItem>();
// 					foreach (FTK_loreItem ftk_loreItem in FTK_loreItemDB.GetDB().m_Array)
// 					{
// 						if (ftk_loreItem.m_Category.ToString() == ftk_loreCategory2.m_ID)
// 						{
// 							FTK_statistic statistic = ftk_loreItem.GetStatistic();
// 							if (dictionary3.ContainsKey(statistic.m_RevealStat))
// 							{
// 								dictionary3[statistic.m_RevealStat].Add(ftk_loreItem);
// 							}
// 							else
// 							{
// 								list3.Add(ftk_loreItem);
// 							}
// 						}
// 					}
// 					List<FTK_loreItem> list4 = new List<FTK_loreItem>();
// 					foreach (string text in dictionary3.Keys)
// 					{
// 						list4.AddRange(dictionary3[text]);
// 					}
// 					list4 = list4.OrderBy((FTK_loreItem o) => o.m_LoreCost).ToList<FTK_loreItem>();
// 					list3 = list3.OrderBy((FTK_loreItem o) => o.m_LoreCost).ToList<FTK_loreItem>();
// 					List<FTK_loreItem> list5 = new List<FTK_loreItem>(list4);
// 					list5.AddRange(list3);
// 					if (list5.Count > 0)
// 					{
// 						if (num == 0)
// 						{
// 							id = FTK_loreCategory.GetEnum(ftk_loreCategory2.m_ID);
// 							dictionary2.Add(id, new List<FTK_loreItem.ID>());
// 						}
// 						foreach (FTK_loreItem ftk_loreItem2 in list5)
// 						{
// 							if (!ftk_loreItem2.m_Ignore)
// 							{
// 								if (!ftk_loreItem2.m_IsCheckCloud || ftk_loreItem2.IsCloudAvailable() || ftk_loreItem2.IsPurchased())
// 								{
// 									if (ftk_loreItem2.m_DLC != FTK_dlc.ID.None)
// 									{
// 										FTK_dlc ftk_dlc = FTK_dlcDB.Get(ftk_loreItem2.m_DLC);
// 										if (!FTK_dlcDB.IsObtainable(ftk_dlc) && !ftk_dlc.IsPurchased())
// 										{
// 											continue;
// 										}
// 									}
// 									dictionary2[id].Add(FTK_loreItem.GetEnum(ftk_loreItem2.m_ID));
// 								}
// 							}
// 						}
// 					}
// 					num++;
// 				}
// 			}
// 			this.CreateItemCardsWithNavigation(dictionary2);
// 			FTKInput.Instance.SetFocus(this, null, true, new Action(uiStartGame.Instance.ShowStartPage), false);
// 			if (this.m_AllCards.Count > 0)
// 			{
// 				this.m_AllCards[0].Select();
// 				FTKInput.SetSelected(this.m_AllCards[0].GetComponent<FTKSelectable>());
// 			}
// 			this.m_Cancel = new FTKInputFocus.OnCancel(base.Close);
// 		}

// 		// Token: 0x06002E56 RID: 11862 RVA: 0x000B3D68 File Offset: 0x000B2168
// 		private void CreateItemCardsWithNavigation(Dictionary<FTK_loreCategory.ID, List<FTK_loreItem.ID>> _items)
// 		{
// 			List<Transform> list = new List<Transform>();
// 			List<Transform> list2 = new List<Transform>();
// 			Transform transform = null;
// 			foreach (FTK_loreCategory.ID id in _items.Keys)
// 			{
// 				GameObject gameObject = Object.Instantiate<GameObject>(this.m_RowPrefab);
// 				gameObject.transform.SetParent(this.m_LoreRoot, false);
// 				string text = FTKHub.Localized<TextMisc>(FTK_loreCategoryDB.Get(id).m_DisplayName);
// 				gameObject.GetComponentInChildren<Text>().text = text;
// 				foreach (FTK_loreItem.ID id2 in _items[id])
// 				{

#endregion
