using System.Collections.Generic;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public static class CombatUtils
    {
        public static CharacterDummy GetDummyInCombat(FTKPlayerID id)
        {
            foreach (KeyValuePair<FTKPlayerID, CharacterDummy> dummy in EncounterSession.Instance.m_Dummies)
            {
                if (dummy.Key == id)
                {
                    return dummy.Value;
                }
            }
            return null;
        }

        public static string GetEnemyName(EnemyDummy dummy)
        {
            if (!uiEnemyHUD.Instance.m_EnemyHudDictionary.ContainsKey(dummy))
            {
                Plugin.Logger.LogError($"invalid dummy ui {dummy?.m_EnemyCombat?.GetEnemyDisplay()}");
                return "";
            }
            uiEachEnemyHud hud = uiEnemyHUD.Instance.m_EnemyHudDictionary[dummy];
            string name = hud.m_EnemyNameDisplay.text;
            return StringReplace.ReplaceNewLineSpace(name);
        }

        public static string GetDungeonSlotLegend(CharacterOverworld cow, VoteButton.VoteOption option)
        {

// dioramaDungeon.m_ActiveTrap.DisarmSlotsAndLegend(this.m_Hud.m_Cow);

// FTK_slotOutput entry = FTK_slotOutputDB.GetDB().GetEntry(this.GetDisarmOutput(_cow));
// uiSlotLegend.Instance.InitializeLegend(_cow, this.GetDisarmOutput(_cow), entry.m_TestSkill);

// MessageCoordinator.Instance.ShowSlotLegend(true, _cow, _outputID, _skill);

// FTKPlayerID ftkplayerID = FTKPlayerID.Null;
// int num = 0;
// if (_cow)
// {
//     ftkplayerID = _cow.m_FTKPlayerID;
//     num = _cow.m_CharacterStats.SpentFocus;
// }

// uiSlotLegend.Instance.InitializeLegendLocal(_open, _cowID, _spentFocus, _output, _skill);

// FTK_slotOutput entry = FTK_slotOutputDB.GetDB().GetEntry(_output);
// switch (entry.m_Category) case FTK_slotOutput.SlotCategory.Dungeon:
// MiniHexDungeon miniHexDungeon = (MiniHexDungeon)this.m_Cow.GetPOI();
// id = FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfDungeon(miniHexDungeon.m_ID, miniHexDungeon.GetDungeonType(), miniHexDungeon.m_HexLand.m_HexInfo.m_Realm, miniHexDungeon.m_HexLand.m_HexInfo.m_StageIndex, miniHexDungeon.m_Level, miniHexDungeon.m_InstanceID);
// FTKUI.Instance.m_PlayerSlots.SetPreSlotRoll(entry.m_SlotAmount, _spentFocus, _skill.ToString(), false, true);
// FTK_slotOutput entry2 = FTK_slotOutputDB.GetDB().GetEntry(_output);
// string text = FTKHub.Localized<TextMisc>(entry2.m_LegendTitle);
// this.m_DisplayTitle.text = text;
// int num = entry2.m_SlotAmount + 1;
// this.m_AllSlotOutputs = new uiSlotLegend.SlotOutput[num];
// this.m_LegendOn = true;
// this.m_DisplayRoot.gameObject.SetActive(true);
// for (int i = 0; i < this.m_AllSlotOutputs.Length; i++)
// {
//     this.m_AllSlotOutputs[i] = this.GetSlotOutputResult(i, _output, id);
// }
// float num2 = 0f;
// if (cow.m_HexLand.m_POI)
// {
//     num2 = cow.m_HexLand.m_POI.GetSkillModifier(_output, cow);
// }
// this.DisplayEachOutput(this.m_AllSlotOutputs, cow.m_CharacterStats.GetSkillValue(_skill, true, num2), _spentFocus);


            // Dictionary<string, Dictionary<string, string>> outcome = [];

            // outcome = RollSlotOutcomes.GetOutcomes(id);
            // // { "ambush": { 0: {5%: failure} }, { 1: {5%: success} }
            // rollData.Add(btn.m_ButtonText.text, outcome);

            // float skillMod = cow.m_HexLand.m_POI.GetSkillModifier(FTK_slotOutput.ID.None, cow);
            // float skillRoll = cow.m_CharacterStats.GetSkillValue(_skill, true, skillMod);

            // float percent = RollSlotOutcomes.GetRollPercent(0, skillRoll, 0);
            // outcome["1"] = RollSlotOutcomes.RollResult(percent, new uiSlotLegend.SlotOutput());



            string result = "";
            // MiniHexDungeon dungeon = (MiniHexDungeon)cow.GetPOI();
            // DioramaDungeon diorama = EncounterSession.Instance.GetDioramaDungeon();
            // FTK_progressionTier.ID progID = FTK_progressionTier.ID.None;
            // MiniHexDungeon.EncounterType type = dungeon.m_EncounterType;
            // switch (type)
            // {
            //     case MiniHexDungeon.EncounterType.Trap1:
            //     case MiniHexDungeon.EncounterType.Trap2:
            //     case MiniHexDungeon.EncounterType.Trap3:
            //         if (option == VoteButton.VoteOption.Disarm)
            //         {
            //             FTK_slotOutput.ID id = diorama.m_ActiveTrap.GetDisarmOutput(cow);
            //             FTK_slotOutput entry = FTK_slotOutputDB.GetDB().GetEntry(id);
            //             if (entry.m_Category != FTK_slotOutput.SlotCategory.Dungeon)
            //             {
            //                 break;
            //             }
            //             progID = FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfDungeon(dungeon.m_ID, dungeon.GetDungeonType(), dungeon.m_HexLand.m_HexInfo.m_Realm, dungeon.m_HexLand.m_HexInfo.m_StageIndex, dungeon.m_Level, dungeon.m_InstanceID);
            //         }
            //         else if (option == VoteButton.VoteOption.Proceed)
            //         {
                        
            //         }
            //         break;
            //     default:
            //         if (type != MiniHexDungeon.EncounterType.Door)
            //         {
            //             if (type != MiniHexDungeon.EncounterType.DungeonMiniEncounter)
            //             {
                            
            //             }
            //             else if (option == VoteButton.VoteOption.Attempt || option == VoteButton.VoteOption.Unlocked || option == VoteButton.VoteOption.Open)
            //             {
                            
            //             }
            //         }
            //         else if (option == VoteButton.VoteOption.Knockdown)
            //         {
                        
            //         }
            //         break;
            // }
            // FTK_slotOutput entry2 = FTK_slotOutputDB.GetDB().GetEntry(diorama);
            // uiSlotLegend.SlotOutput[] outputs = new uiSlotLegend.SlotOutput[entry2.m_SlotAmount+1];
            // for (int i = 0; i < outputs.Length; i++)
            // {
            //     outputs[i] = uiSlotLegend.Instance.GetSlotOutputResult(i, diorama, progID);
            // }
            // float skillMod = 0f;
            // if (cow.m_HexLand.m_POI)
            // {
            //     skillMod = cow.m_HexLand.m_POI.GetSkillModifier(diorama, cow);
            // }
            // float skillRoll = cow.m_CharacterStats.GetSkillValue(entry2.m_TestSkill, true, skillMod);

            return result;
        }

    }
}