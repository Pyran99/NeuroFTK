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
            gr,
            lc,
        }
        public static Adventure currentAdventure = Adventure.ftk;
        /// <summary>
        /// vector 2 string from HexData
        /// </summary>
        public static readonly Dictionary<string, HexLand> questHexes = [];

        static readonly List<Vector3> questPositions = [];
        static StringBuilder sbQuest = new();


        public static string GetQuestData()
        {
            questHexes.Clear();
            questPositions.Clear();
            sbQuest = new();
            Vector3 cowHex = CharacterData.GetActiveCow().GetHexLand().GetPosition();
            List<uiQuestItem> allQuests = [.. uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>()];
            allQuests.AddRange(uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren<uiQuestItem>());
            foreach (uiQuestItem q in allQuests)
            {
                AddValidQuests(q, cowHex);
            }
            if (sbQuest.Length > 0) sbQuest.Insert(0, "## active quests \n");
            if (currentAdventure == Adventure.dc) HandleDungeonCrawlQuest();
            return sbQuest.ToString();
        }

        static void AddValidQuests(uiQuestItem questItem, Vector3 cowHex)
        {
            if (StringReplace.RemoveStyling(questItem.m_Display.text) == "??????") return;
            if (questItem.m_IsComplete) return;
            QuestLogicBase quest = questItem.m_Quest;
            if (quest == null) return;
            if (quest.IsRawComplete()) return;
            string type = quest.HasQuestDefID() ? "main" : "side";
            string description = StringReplace.RemoveStyling(quest.GetLocalizedOneLineDesc());
            HexLand dest = quest.GetHexLandDestination();
            if (dest != null)
            {
                string pos = HexData.GetVec2String(dest); // inaccurate position
                if (questHexes.ContainsKey(pos)) return;
                if (dest.GetPosition() == cowHex)
                {
                    questHexes.Add(pos, dest);
                    questPositions.Add(dest.GetPosition());
                    sbQuest.AppendLine($"- {type} quest at {pos}: {description} (you are currently at this hex)");
                    return;
                }
                string outOfRange = "";
                if ((dest.GetPosition() - cowHex).magnitude > GlobalConfig.maxDistance)
                {
                    outOfRange = " (out of pathfinding range)";
                }
                questHexes.Add(pos, dest);
                questPositions.Add(dest.GetPosition());
                string boat = GetBoatTravelCtx(description);
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
            Vector3 cowPos = cow.GetHexLand().GetPosition();
            Vector2 vec2 = new();
            foreach (Vector3 pos in positions)
            {
                if (pos == cowPos) continue;
                if ((pos - cowPos).magnitude < GlobalConfig.maxDistance)
                {
                    vec2.x = (int)pos.x;
                    vec2.y = (int)pos.z;
                    result.Add(HexData.Vec2ToString(vec2));
                }
            }
            if (currentAdventure == Adventure.dc)
            {
                IEnumerable<MiniHexDungeon> dungeons = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Dungeon).Cast<MiniHexDungeon>();
                string pos;
                foreach (MiniHexDungeon d in dungeons)
                {
                    if (d.GetDungeonType() == MiniHexDungeon.DungeonType.Main)
                    {
                        if (d.IsDungeonCleared()) continue;
                        pos = HexData.GetVec2String(d.m_HexLand);
                        if (result.Contains(pos)) continue;
                        if ((d.m_HexLand.GetPosition() - cowPos).magnitude < GlobalConfig.maxDistance)
                        {
                            result.Add(pos);
                        }
                    }
                }
            }
            return result;
        }

        static void HandleDungeonCrawlQuest()
        {
            IEnumerable<uiQuestItem> mainQuests = uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>();
            string description = StringReplace.RemoveStyling(mainQuests.First().m_Display.text);
            sbQuest.AppendLine($"main quest: {description}. ");
            CharacterOverworld cow = CharacterData.GetActiveCow();
            Vector3 cowHex = cow.GetHexLand().GetPosition();
            IEnumerable<MiniHexDungeon> dungeons = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Dungeon).Cast<MiniHexDungeon>();
            sbQuest.AppendLine("## dungeon locations ");
            HexLand dest;
            Vector3 pos;
            string outOfRange;
            string boat;
            foreach (MiniHexDungeon d in dungeons)
            {
                if (d.GetDungeonType() == MiniHexDungeon.DungeonType.Main)
                {
                    outOfRange = "";
                    dest = d.m_HexLand;
                    pos = dest.GetPosition();
                    if (questPositions.Contains(pos)) continue;
                    questPositions.Add(pos);
                    questHexes.Add(HexData.GetVec2String(dest), dest);
                    if ((pos - cowHex).magnitude > GlobalConfig.maxDistance)
                    {
                        outOfRange = " (out of pathfinding range)";
                    }
                    boat = GetBoatTravelCtx(dest.GetRealmDisplayValue());
                    // dest.GetRealm();
                    sbQuest.AppendLine($"- {HexData.GetVec2String(dest)}{outOfRange}{boat}.");
                }
            }
        }

        public static string GetAdventuresMainQuestCtx(Adventure adventure, bool addLocations = false)
        {
            return adventure switch
            {
                Adventure.ftk => GetMainQuestCtx(addLocations),
                Adventure.fa => GetMainQuestCtx(addLocations),
                Adventure.id => GetMainQuestCtx(addLocations),
                Adventure.dc => GetDungeonCrawlQuestHelper(addLocations),
                Adventure.hc => GetMainQuestCtx(addLocations),
                Adventure.gr => GetGoldRushCtx(addLocations),
                _ => GetMainQuestCtx(addLocations),
            };
        }

        public static string GetMainQuestCtx(bool addLocations = false)
        {
            return StringMessages.OverworldReminderCtx; // quest info is already sent in actions
            // List<uiQuestItem> allQuests = [.. uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>()];
            // allQuests.AddRange(uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren<uiQuestItem>());
            // // IEnumerable<QuestLogicBase> quests = GameLogic.Instance.GetQuestTable().Values;
            // StringBuilder sb = new();
            // sb.AppendLine("## quest locations ");
            // foreach (uiQuestItem q in allQuests)
            // {
            //     // if (!q.HasQuestDefID()) continue;
            //     Plugin.Logger.LogWarning($"{q.m_Quest.m_StoryQuestID} : {q.m_Quest.GetLocalizedOneLineDesc()}");
            //     if (addLocations && q.m_Quest.GetHexLandDestination() != null)
            //     {
            //         string type = q.m_Quest.HasQuestDefID() ? "main" : "side";
            //         sb.AppendLine($"- {type}: {HexData.GetVec2Pos(q.m_Quest.GetHexLandDestination())}");
            //     }
            // }
            // return sb.ToString();
        }

        public static string GetDungeonCrawlQuestHelper(bool addLocations = false)
        {
            return $"{StringMessages.OverworldReminderCtx} you need to find and clear all 5 main quest dungeons to win. there is 1 dungeon in each realm.";
            // //side quests handled from normal data
            // IEnumerable<uiQuestItem> allQuests = uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>();
            // string description = StringReplace.RemoveStyling(allQuests.First().m_Display.text);
            // IEnumerable<MiniHexDungeon> dungeons = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Dungeon).Cast<MiniHexDungeon>();
            // StringBuilder sb = new($"main quest: {description} \n");
            // if (addLocations)
            // {
            //     sb.AppendLine("## quest locations");
            //     foreach (MiniHexDungeon d in dungeons)
            //     {
            //         if (d.GetDungeonType() == MiniHexDungeon.DungeonType.Main)
            //         {
            //             sb.AppendLine($"- {HexData.GetVec2Pos(d.m_HexLand)}");
            //         }
            //     }
            // }
            // return sb.ToString();
        }

        static string GetGoldRushCtx(bool addLocations = false)
        {
            StringBuilder sb = new();
            float gold = FTKUtil.RoundToInt(GameFlow.Instance.m_Rules.GetParams()[GridEditor.FTK_gameParams.ID.deliver_gold]);
            // string gold = GameFlow.Instance.GameDif.GetGoldDeliverText(GameFlow.Instance.m_Rules); // Gold Target: {0}
            sb.Append($"explore the map to gather gold. if any character has {gold} gold, you should move them to the main quest location to win.");
            return sb.ToString();
        }

        static string GetBoatTravelCtx(string description)
        {
            if (HexData.IsBoatRequired(description))
            {
                if (currentAdventure == Adventure.ftk) return " (may require boat (can be bought at port), or an airship)";
                // else if (currentAdventure == Adventure.dc) return "";
                return " (may require boat (can be bought at port)";
            }
            else if (HexData.IsAirshipRequired(description)) return " (requires an airship to reach)";
            return "";
        }

    }
}