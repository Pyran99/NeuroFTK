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
            string result = "";
            Dictionary<string, Dictionary<string, string>> data = [];
            MiniHexDungeon dungeon = (MiniHexDungeon)cow.GetPOI();
            DioramaDungeon diorama = EncounterSession.Instance.GetDioramaDungeon();
            FTK_progressionTier.ID progID = FTK_progressionTier.ID.None;
            MiniHexDungeon.EncounterType type = dungeon.m_EncounterType;
            FTK_slotOutput entry = null;
            FTK_slotOutput.ID outputId = FTK_slotOutput.ID.None;
            switch (type)
            {
                case MiniHexDungeon.EncounterType.Trap1:
                case MiniHexDungeon.EncounterType.Trap2:
                case MiniHexDungeon.EncounterType.Trap3:
                    if (option == VoteButton.VoteOption.Disarm)
                    {
                        outputId = diorama.m_ActiveTrap.GetDisarmOutput(cow);
                        entry = FTK_slotOutputDB.GetDB().GetEntry(outputId);
                        if (entry.m_Category != FTK_slotOutput.SlotCategory.Dungeon) break;
                        progID = FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfDungeon(dungeon.m_ID, dungeon.GetDungeonType(), dungeon.m_HexLand.m_HexInfo.m_Realm, dungeon.m_HexLand.m_HexInfo.m_StageIndex, dungeon.m_Level, dungeon.m_InstanceID);
                    }
                    else if (option == VoteButton.VoteOption.Proceed)
                    {
                        
                    }
                    break;
                default:
                    if (type != MiniHexDungeon.EncounterType.Door)
                    {
                        if (type != MiniHexDungeon.EncounterType.DungeonMiniEncounter)
                        {
                            
                        }
                        else if (option == VoteButton.VoteOption.Attempt || option == VoteButton.VoteOption.Unlocked || option == VoteButton.VoteOption.Open)
                        {
                            
                        }
                    }
                    else if (option == VoteButton.VoteOption.Knockdown)
                    {
                        
                    }
                    break;
            }
            //TODO this part done?
            RollSlotOutcomes.SetSlotLegendResult(entry, outputId, progID, cow, ref data);
            // foreach (KeyValuePair<string, Dictionary<string, string>> outcome in data)
            // {
            //     result += $"[{outcome.Key}]\n";
            //     foreach (KeyValuePair<string, string> value in outcome.Value)
            //     {
            //         result += $"{value.Key}({value.Value})\n";
            //     }
            // }
            return result;
        }

        static void DisarmData()
        {
            
        }

        static void ProceedData()
        {
            
        }

        static void AttemptData() //attempt, unlocked, open
        {
            
        }

        static void BashData()
        {
            
        }

    }
}