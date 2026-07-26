using Pyran.NeuroFTK.HarmonyPatches;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class HexData
    {
        public static Vector2 GetVec2Pos(HexLand hex)
        {
            Vector3 pos = hex.GetPosition();
            return new Vector2(pos.x, pos.z);
        }

        public static bool IsPoiComplete(MiniHexInfo poi)
        {
            if (poi == null) return true;
            if (poi.m_Deactivated) return true;
            return false;
        }

        public static bool IsUsedDeactivateCtx(MiniHexInfo.MiniHexType type)
        {
            return type == MiniHexInfo.MiniHexType.Dungeon || type == MiniHexInfo.MiniHexType.Sanctum || type == MiniHexInfo.MiniHexType.StoneHero;
        }

        public static bool CanTravel(HexLand hex, CharacterOverworld cow)
        {
            bool isLand = hex.m_Type == HexLand.Type.Land;
            bool cowOnLand = cow.GetHexLand().m_Type == HexLand.Type.Land;
            bool onBoat = cow.IsInBoat();
            //if hex is land & cow on land => land=>land
            if (isLand && cowOnLand) return true;
            //if hex is land & cow on boat => boat=>land
            if (isLand && onBoat) return true;
            //if hex is water & cow on land => land=>water
            if (!isLand && cowOnLand) return false;
            //if hex is water & cow on boat => boat=>water
            if (!isLand && onBoat) return true;
            //if hex has boat & cow on land => land=>boat
            if (hex.IsBoat() && cowOnLand) return true;
            // what would 2 boats do
            // what about air
            return false;
        }

        public static QuestLogicBase TileHasQuestObjective(HexLand hex)
        {
            MiniHexInfo poi = hex.GetPOI();
            if (poi?.HasEncounterQuest() ?? false)
            {
                return poi.GetEncounterQuest();
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addToList">adds to member OverworldFlow.hexPositions</param>
        /// <returns>[(155.8, 20.0): (The Guardian Forest)(). Woodsmoke]</returns>
        public static string GetContextForHex(CharacterOverworld cow, HexLand hex, bool addToList = false)
        {
            // FTK_realm.ID realm;
            // realm = hex.GetRealm();
            // GuardianForest | GoldenPlains
            // Plugin.Logger.LogWarning("realm: " + realm);
            // distance = (float)Math.Round(HexLand.Distance(cow.m_HexLand, hex), 2);
            string poi = "";
            string hasDeadPlayers = "";
            string questName = "";
            string name = hex.GetLocationDisplayValue(cow);
            Vector2 pos = GetVec2Pos(hex);
            QuestLogicBase _quest = TileHasQuestObjective(hex);
            if (_quest != null && !_quest.IsConsiderComplete())
            {
                if (_quest.HasQuestDefID())
                {
                    // _quest.m_StoryQuestID 
                    questName = "story quest";
                }
                else questName = "quest location";
                // quest.GetCurrentDestinationLocation();
            }
            if (hex.GetDeadPlayerCount() > 0)
            {
                hasDeadPlayers = "has dead character to revive";
            }
            MiniHexInfo hexInfo = hex.GetPOI();
            if (hexInfo != null)
            {
                poi = hexInfo.GetPOIDisplayValue() + ": " + hexInfo.m_MiniHexType;
                if (IsPoiComplete(hexInfo))
                {
                    poi += " (completed)";
                }
            }
            if (addToList) OverworldFlow.hexPositions.Add(pos.ToString(), hex);
            return $"[{pos} ({name})({questName}){hasDeadPlayers + ". "}{poi}]";
        }
    }
}