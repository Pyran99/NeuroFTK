using System.Collections.Generic;
using System.Text;
using Pyran.NeuroFTK.GameConfigs;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class QuestHelper
    {
        public static readonly Dictionary<string, QuestLogicBase> questDict = [];

        static readonly List<Vector3> questPositions = [];
        static StringBuilder sbQuest = new();


        public static string GetQuestData()
        {
            questDict.Clear();
            questPositions.Clear();
            sbQuest = new();
            Vector3 cowHex = CharacterData.GetActiveCow().GetHexLand().GetPosition();
            foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>())
            {
                AddValidQuests(q, cowHex);
            }
            foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren<uiQuestItem>())
            {
                AddValidQuests(q, cowHex);
            }
            if (sbQuest.Length > 0) sbQuest.Insert(0, "## active quests \n");
            return sbQuest.ToString();
        }

        static void AddValidQuests(uiQuestItem questItem, Vector3 cowHex)
        {
            if (StringReplace.RemoveStyling(questItem.m_Display.text) == "??????") return;
            if (questItem.m_IsComplete) return;
            QuestLogicBase quest = questItem.m_Quest;
            if (quest == null) return;
            if (quest.IsRawComplete()) return;
            string type = "side";
            if (quest.HasQuestDefID()) type = "main"; // only story quest ids
            string description = StringReplace.RemoveStyling(quest.GetLocalizedOneLineDesc());
            HexLand dest;
            dest = quest.GetHexLandDestination();
            if (dest != null)
            {
                Vector2 pos = HexData.GetVec2Pos(dest);
                if (questDict.ContainsKey(pos.ToString())) return;
                if (dest.GetPosition() == cowHex)
                {
                    questDict.Add(pos.ToString(), quest);
                    questPositions.Add(dest.GetPosition());
                    sbQuest.AppendLine($"[{type} quest at {pos}]: {description} (you are currently at this hex)");
                    return;
                }
                string outOfRange = "";
                if ((dest.GetPosition() - cowHex).magnitude > GlobalConfig.maxDistance)
                {
                    outOfRange = " (out of pathfinding range)";
                }
                questDict.Add(pos.ToString(), quest);
                questPositions.Add(dest.GetPosition());
                string boat = "";
                if (HexData.IsBoatRequired(description)) boat = " (may require boat (can be bought at port), or an airship)";
                else if (HexData.IsAirshipRequired(description)) boat = " (requires an airship to reach)";
                sbQuest.AppendLine($"[{type} quest at {pos}]: {description}{outOfRange}{boat}");
                // quest desc: Kill the Chaos Leader in The Guardian Forest
                // quest pos: (85.1, 117.5)
            }
        }

        public static QuestLogicBase TileHasQuestObjective(HexLand hex)
        {
            QuestLogicBase quest = HexData.TileHasQuestObjective(hex);
            if (quest != null) return quest;
            if (questPositions.Contains(hex?.GetPosition() ?? Vector3.positiveInfinity))
            {
                return GameLogic.Instance.GetQuestByID(questPositions.IndexOf(hex.GetPosition()));
            }
            return null;
        }

        public static List<Vector3> GetQuestPositions()
        {
            return questPositions;
        }

        public static List<string> GetInRangeQuests(CharacterOverworld cow)
        {
            List<string> result = [];
            List<Vector3> positions = GetQuestPositions();
            foreach (KeyValuePair<string, QuestLogicBase> kvp in questDict)
            {
                Vector3 dest = kvp.Value.GetHexLandDestination()?.GetPosition() ?? Vector3.positiveInfinity;
                Vector3 cowPos = cow.GetHexLand().GetPosition();
                if (dest == cowPos) continue;
                if (positions.Contains(dest))
                {
                    if ((dest - cowPos).magnitude < GlobalConfig.maxDistance)
                    {
                        result.Add(kvp.Key);
                    }
                }
            }
            return result;
        }
        
    }
}