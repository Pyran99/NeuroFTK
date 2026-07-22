using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public static class CombatUtils
    {
        public static FTK_slotOutput entry;

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

        /// <returns>example => 0(2%) = Failure</returns>
        public static string GetDungeonSlotLegend(CharacterOverworld cow, VoteButton.VoteOption option)
        {
            if (!cow.IsInDungeon()) return "";
            MiniHexDungeon dungeon = (MiniHexDungeon)cow.GetPOI();
            DioramaDungeon diorama = EncounterSession.Instance.GetDioramaDungeon();
            MiniHexDungeon.EncounterType type = dungeon.m_EncounterType;
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
                    }
                    else if (option == VoteButton.VoteOption.Proceed)
                    {
                        outputId = diorama.m_ActiveTrap.GetProceedOutput(cow);
                        entry = FTK_slotOutputDB.GetDB().GetEntry(outputId);
                    }
                    break;
                default:
                    if (type != MiniHexDungeon.EncounterType.Door)
                    {
                        if (type != MiniHexDungeon.EncounterType.DungeonMiniEncounter){} // needed?
                        else if (option == VoteButton.VoteOption.Attempt || option == VoteButton.VoteOption.Unlocked || option == VoteButton.VoteOption.Open)
                        {
                            outputId = diorama.m_DungeonEncounter.m_EncounterObject.GetDBEntry().m_SlotRoll;
                            if (outputId != FTK_slotOutput.ID.None)
                            {
                                entry = FTK_slotOutputDB.GetDB().GetEntry(outputId);
                            }
                        }
                    }
                    else if (option == VoteButton.VoteOption.Knockdown)
                    {
                        outputId = diorama.m_DoorToBash.GetComponent<DungeonDoor>().GetDoorBashOutput(cow);
                        entry = FTK_slotOutputDB.GetDB().GetEntry(outputId);
                    }
                    break;
            }
            Dictionary<string, Dictionary<string, string>> data = [];
            RollSlotOutcomes.SetSlotLegendResult(entry, outputId, cow, ref data);
            StringBuilder sb = new();
            foreach (KeyValuePair<string, Dictionary<string, string>> outcome in data)
            {
                // 0(2%) = Failure <= for each roll
                sb.AppendLine($"{outcome.Key}({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}");
            }
            return sb.ToString();
        }
    }
}