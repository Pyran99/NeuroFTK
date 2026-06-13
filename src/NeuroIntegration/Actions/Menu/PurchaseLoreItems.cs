using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FTKItemName;
using Google2u;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Newtonsoft.Json;
using Pyran.NeuroFTK.GameConfigs;
using StartGameFE;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PurchaseLoreItems(uiLoreStore store, List<uiLoreCard> cards) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(uiLoreStore instance, List<uiLoreCard> cards)
        {
            string json = JsonConvert.SerializeObject(LoreStoreUnlocks.GetCategoryData(), Formatting.None);
            json = StringReplace.ReplaceNewLine(json);
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            PurchaseLoreItems action = new(instance, cards);
            action.itemPurchased += LoreStoreUnlocks.OnItemPurchased;
            window.SetContext($"lore store category details: {json}");
            window.SetForce(2, "purchase lore items from a category or cancel the action and go back to the main menu if you dont want to purchase anything right now", "You are in the lore store for game unlocks");
            window.AddAction(action);
            CancelAction cancelAction = new(window, "return to main menu");
            cancelAction.OnCancelled += LoreStoreUnlocks.OnActionCancelled;
            window.AddAction(cancelAction);
            window.Register();
            return window;
        }

        public uiLoreStore uiLoreStore = store;
        public List<uiLoreCard> uiLoreCards = cards;
        public Action<PurchaseLoreItems> itemPurchased;
        public static bool isPurchasing = false;
        // {"night market": {"description": "", "card": LoreCard}}
        readonly Dictionary<string, Dictionary<string, object>> availableLoreData = [];

        public override string Name => "purchase_lore_item";
        protected override string Description => "purchase an item from the store. these unlock various things that can appear in future runs.";
        protected override JsonSchema Schema => GetSchema();

        protected override void Execute(string parsedData)
        {
            if (isPurchasing)
            {
                Plugin.Logger.LogWarning("duplicate store purchase");
                return;
            }
            isPurchasing = true;
            uiLoreStore.StartCoroutine(DoPurchase(availableLoreData[parsedData]["card"] as uiLoreCard, parsedData));
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            if (!actionData.Data.Contains("item")) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(["item"]));
            string result = actionData.Data.Value<string>("item");
            if (!availableLoreData.ContainsKey(result))
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            }
            parsedData = result;
            return ExecutionResult.Success();
        }

        JsonSchema GetSchema()
        {
            List<string> data;
            Dictionary<string, string> schemaData = GetAllItemsDetails(uiLoreCards);
            string context = "";
            foreach (string key in schemaData.Keys)
            {
                context += $"{{name: {key}, description: {StringReplace.ReplaceNewLine(schemaData[key])}}}. ";
            }
            Context.Send($"Items and their descriptions you can afford: {context}");
            data = [.. schemaData.Select(l => l.Key)];
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["item"],
                Properties = new()
                {
                    ["item"] = QJS.Enum(data),
                }
            };
            return schema;
        }

        private IEnumerator DoPurchase(uiLoreCard card, string itemName, float delay = 1.0f)
        {
            card.Select();
            bool failedPurchase = false;
            if (card.m_LoreItem.IsPurchased())
            {
                Plugin.Logger.LogWarning($"card {card.m_LoreItem.m_ID} is already purchased");
                failedPurchase = true;
            }
            if (!card.m_LoreItem.CanAfford())
            {
                Plugin.Logger.LogWarning($"cannot afford {card.m_LoreItem.m_ID}");
                failedPurchase = true;
            }
            if (!card.m_LoreItem.IsRevealed())
            {
                Plugin.Logger.LogWarning($"card {card.m_LoreItem.m_ID} is not revealed");
                failedPurchase = true;
            }
            if (failedPurchase)
            {
                Context.Send($"there was an issue purchasing the store item {itemName}, going back to the main menu{NeuroSdkStrings.ModFaultSuffix}");
                uiLoreStore.OnClose();
                isPurchasing = false;
                yield break;
            }
            string successMsg = $"you purchased {itemName}";
            foreach (KeyValuePair<string, Dictionary<string, object>> item in availableLoreData)
            {
                if (item.Key.ToLower() != itemName.ToLower()) continue;
                successMsg = $"you purchased: '{item.Key}' '{item.Value["description"]}'";
                break;
            }
            Context.Send(successMsg);
            yield return new WaitForSeconds(delay);
            if (!GlobalConfig.debug_mode) card.CommitToLorePurchase(); // skips confirm popup, debug_mode skips purchase fully
            yield return new WaitForSeconds(delay);
            itemPurchased.Invoke(this);
            isPurchasing = false;
        }

        // get every item that can be purchased
        private Dictionary<string, string> GetAllItemsDetails(List<uiLoreCard> cards)
        {
            Dictionary<string, string> loreData = [];
            Dictionary<string, string> entry;
            FTK_loreItem item;
            foreach (uiLoreCard card in uiLoreCards)
            {
                item = card.m_LoreItem;
                if (!item.IsRevealed() || item.IsPurchased() || !item.CanAfford()) continue;
                if (item.m_Category != FTK_loreCategory.ID.items)
                {
                    // ShowOtherLoreItem
                    entry = GetItemIdAndDescription(item);
                }
                else
                {
                    // this.m_ItemDetail.Show(_itemID, uiItemDetail.Mode.ItemDisplay, _cow, false, _forceFrontSide, _loreCard);
                    FTK_itembase itemBase = FTK_itembase.GetItemBase((FTK_itembase.ID)item.m_UnlockID);
                    // string trName = itemBase.GetLocalizedName();
                    entry = HandleEquipmentDetails((FTK_itembase.ID)item.m_UnlockID);
                }
                string key = entry.Keys?.First().ToLower();
                // some item sets use the same name
                if (item.m_Category == FTK_loreCategory.ID.extraArmor || item.m_Category == FTK_loreCategory.ID.extraBackpack || item.m_Category == FTK_loreCategory.ID.extraHelmet || item.m_Category == FTK_loreCategory.ID.extraSkin)
                {
                    key = item.m_ID;
                }
                if (loreData.ContainsKey(key))
                {
                    Plugin.Logger.LogWarning($"duplicate key found {key}");
                    continue;
                }
                key = FixName(key);
                string value = entry.Values?.First();
                loreData.Add(key, value);
                Dictionary<string, object> _value = new()
                {
                    {"description", value},
                    {"card", card}
                };
                availableLoreData.Add(key, _value);
            }
            return loreData;
        }

        private Dictionary<string, string> GetItemIdAndDescription(FTK_loreItem item)
        {
            Dictionary<string, string> data = [];
            string id = "";
            string description = GetTrItemDescription(item);
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
                    description = "";
                    break;
            }
            data.Add(id, description);
            return data;
        }

        // {name: description}
        private Dictionary<string, string> HandleEquipmentDetails(FTK_itembase.ID itemId)
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
        private string GetWeaponDetails(FTK_weaponStats2 weaponStats)
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
            string final = $"'weapon damage:{maxDmg} {dmgType}' 'attacks and proficiencies: {text1}' 'modifiers: {text2}'";
            return final;
        }

        private string GetTrItemDescription(FTK_loreItem item)
        {
            if (item.m_Category == FTK_loreCategory.ID.items) return "";
            if (item.m_Category == FTK_loreCategory.ID.classes)
            {
                FTK_playerGameStart entry = FTK_playerGameStartDB.GetDB().GetEntry((FTK_playerGameStart.ID)item.m_UnlockID);
                return FTKHub.Localized<TextCharacters>(entry.m_Flavor);
            }
            return FTKHub.Localized<TextLoreStore>(item.m_CardDescription);
        }

        private string FixName(string name)
        {
            return name switch
            {
                "HelmetMask01" => "HelmetBeastman",
                "HelmetMask02" => "HelmetOwlbear",
                "HelmetMask03" => "HelmetTriclops",
                _ => name,
            };
        }

    }
}


