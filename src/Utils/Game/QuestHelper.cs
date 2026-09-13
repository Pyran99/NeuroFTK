using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pyran.NeuroFTK.GameConfigs;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class QuestHelper
    {
        public enum Adventure
        {
            ftk,
            fa,
            id,
            dc,
            hc,
            gr
        }
        public static Adventure currentAdventure = Adventure.ftk;

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
                    sbQuest.AppendLine($"- {type} quest at {pos}: {description} (you are currently at this hex)");
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
                sbQuest.AppendLine($"- {type} quest at {pos}: {description}{outOfRange}{boat}");
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

        public static void DungeonCrawlQuestHelper()
        {
            //TODO send location of all hidden dungeons for quest. may be cheaty
            IEnumerable<MiniHexDungeon> dungeons = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Dungeon).Cast<MiniHexDungeon>();
            List<Vector2> locations = [.. dungeons.Select(d => HexData.GetVec2Pos(d.m_HexLand))];
            Plugin.Logger.LogWarning($"locations: {string.Join(", ", [.. locations.Select(x => x.ToString())])}");
            Plugin.Logger.LogWarning($"types: {string.Join(", ", [.. dungeons.Select(x => x.GetDungeonType().ToString())])}");
            Plugin.Logger.LogWarning($"locations has quest: {string.Join(", ", [.. dungeons.Where(x => x.HasEncounterQuest()).Select(x => HexData.GetVec2Pos(x.m_HexLand).ToString())])}");
            Dictionary<int, QuestLogicBase> quests = GameLogic.Instance.GetQuestTable();
            // maybe Main type is for quest
// [Warning:Neuro For the King] locations: (127.0, 70.0), (90.9, 22.5), (67.8, 42.5), (89.5, 95.0), (142.9, 97.5), (135.6, 105.0), (109.7, 85.0), (99.6, 87.5), (70.7, 92.5), (77.9, 115.0), (59.2, 57.5), (50.5, 62.5), (128.4, 122.5), (56.3, 77.5), (102.5, 27.5), (96.7, 132.5), (64.9, 87.5), (77.9, 135.0), (140.0, 112.5), (85.1, 82.5)
// [Warning:Neuro For the King] types: Mini, Main, Mini, Mini, Main, Mini, Main, Mini, Main, Mini, Main, Mini, SeaCave, SeaCave, SeaCave, SeaCave, SeaCave, SeaCave, SeaCave, SeaCave
// [Warning:Neuro For the King] locations has quest:
        }
        
    }
}