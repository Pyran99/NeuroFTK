using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public static class CombatUtils
    {
        public static FTK_slotOutput Entry { get; private set; }
        public static FTK_slotOutput.ID OutputId { get; private set; }

        public static void ResetSlotOutput()
        {
            Entry = null;
            OutputId = FTK_slotOutput.ID.None;
        }

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

        /// <returns>example => - 0(2%) = Failure</returns>
        public static string GetDungeonSlotLegend(CharacterOverworld cow, VoteButton btn)
        {
            if (!cow.IsInDungeon() || !cow.m_CharacterStats.m_IsInCombat) return "";
            ResetSlotOutput();
            VoteButton.VoteOption option = btn.m_Option;
            MiniHexDungeon dungeon = (MiniHexDungeon)cow.GetPOI();
            DioramaDungeon diorama = EncounterSession.Instance.GetDioramaDungeon();
            MiniHexDungeon.EncounterType type = dungeon.m_EncounterType;
            switch (type)
            {
                case MiniHexDungeon.EncounterType.Trap1:
                case MiniHexDungeon.EncounterType.Trap2:
                case MiniHexDungeon.EncounterType.Trap3:
                    if (option == VoteButton.VoteOption.Disarm)
                    {
                        OutputId = diorama.m_ActiveTrap.GetDisarmOutput(cow);
                        Entry = FTK_slotOutputDB.GetDB().GetEntry(OutputId);
                    }
                    else if (option == VoteButton.VoteOption.Proceed)
                    {
                        OutputId = diorama.m_ActiveTrap.GetProceedOutput(cow);
                        Entry = FTK_slotOutputDB.GetDB().GetEntry(OutputId);
                    }
                    break;
                default:
                    if (type != MiniHexDungeon.EncounterType.Door)
                    {
                        if (type != MiniHexDungeon.EncounterType.DungeonMiniEncounter){} // needed
                        else if (option == VoteButton.VoteOption.Attempt || option == VoteButton.VoteOption.Unlocked || option == VoteButton.VoteOption.Open)
                        {
                            OutputId = diorama.m_DungeonEncounter.m_EncounterObject.GetDBEntry().m_SlotRoll;
                            if (OutputId != FTK_slotOutput.ID.None)
                            {
                                Entry = FTK_slotOutputDB.GetDB().GetEntry(OutputId);
                            }
                        }
                    }
                    else if (option == VoteButton.VoteOption.Knockdown)
                    {
                        OutputId = diorama.m_DoorToBash.GetComponent<DungeonDoor>().GetDoorBashOutput(cow);
                        Entry = FTK_slotOutputDB.GetDB().GetEntry(OutputId);
                    }
                    break;
            }
            Dictionary<string, Dictionary<string, string>> data = [];
            // Plugin.Logger.LogWarning("testing entry serialize " + Jason.Serialize(Entry));
            if (OutputId != FTK_slotOutput.ID.None)
            {
                RollSlotOutcomes.SetSlotLegendResult(Entry, OutputId, cow, data);
            }
            StringBuilder sb = new();
            foreach (KeyValuePair<string, Dictionary<string, string>> outcome in data)
            {
                // 0(2%) = Failure <= for each roll
                sb.AppendLine($"- {outcome.Key}({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}");
            }
            return sb.ToString();
        }
    }
}