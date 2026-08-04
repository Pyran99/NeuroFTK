using System.Collections.Generic;
using GridEditor;
using NeuroSdk.Messages.Outgoing;
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

        public static bool IsPoiInteractable(MiniHexInfo poi, CharacterOverworld cow)
        {
            if (poi == null) return false;
            if (poi.m_Deactivated) return false;
            bool interactable = false;
            switch (poi.m_MiniHexType)
            {
                case MiniHexInfo.MiniHexType.Town:
                case MiniHexInfo.MiniHexType.Utility:
                case MiniHexInfo.MiniHexType.Portal:
                case MiniHexInfo.MiniHexType.SafeCamp:
                case MiniHexInfo.MiniHexType.FortuneTeller:
                    return true;
                case MiniHexInfo.MiniHexType.Sanctum:
                    return !(poi as MiniHexSanctum).m_SanctumClaimed;
                case MiniHexInfo.MiniHexType.AlluringPool:
                    interactable = IsAlluringPoolInteractable(poi as MiniHexAlluringPool);
                    break;
                case MiniHexInfo.MiniHexType.MiniEncounter:
                    interactable = IsEncounterInteractable(poi as MiniEncounter, cow);
                    break;
                case MiniHexInfo.MiniHexType.Dungeon:
                    interactable = IsDungeonInteractable(poi as MiniHexDungeon, cow);
                    break;
                default:
                    break;
            }
            return interactable;
        }

        public static bool IsPoiCompleted(MiniHexInfo poi, CharacterOverworld cow) // FIXME forgotten cellar was true when not complete
        {
            if (poi == null) return false;
            if (poi.m_Deactivated) return true;
            switch (poi.m_MiniHexType)
            {
                case MiniHexInfo.MiniHexType.Haunt:
                    return (poi as MiniHexHaunt).m_HauntSealed;
                case MiniHexInfo.MiniHexType.Dungeon:
                    return IsDungeonInteractable(poi as MiniHexDungeon, cow);
                case MiniHexInfo.MiniHexType.MiniEncounter:
                    return IsEncounterInteractable(poi as MiniEncounter, cow);
                case MiniHexInfo.MiniHexType.Sanctum:
                    return (poi as MiniHexSanctum).m_SanctumClaimed || (poi as MiniHexSanctum).m_SanctumBroken;
                case MiniHexInfo.MiniHexType.Utility:
                    return (poi as MiniHexUtility).m_UtilityActivated;
                default:
                    return false;
            }
        }

        static bool IsAlluringPoolInteractable(MiniHexAlluringPool poi)
        {
            if (poi.GetAlluringPoolOptions().Count == 0)
            {
                Context.Send("you need to find other alluring pools to activate the teleport system", true);
                return false;
            }
            return true;
        }

        static bool IsEncounterInteractable(MiniEncounter encounter, CharacterOverworld cow)
        {
            Plugin.Logger.LogMessage("poi encounter type = " + encounter.m_Type);
            if (encounter.m_HasBeenConsumed || encounter.m_CantUseThisTurn) return false;
            if (encounter.m_Type == FTK_miniEncounter.ID.kvHome && cow.GetHexLand() == encounter.m_HexLand)
            {
                Context.Send($"{CharacterData.GetCharacterName(cow)} does not have the required quest item for this hex", true);
                return false;
            }
            else if (encounter.m_Type == FTK_miniEncounter.ID.Cellar)
            {
                if (encounter.HasEncounterQuest())
                {
                    QuestLogicBase quest = encounter.GetEncounterQuest();
                    if (quest.IsConsiderComplete()) return true;
                }
            }
            return true;
        }

        static bool IsDungeonInteractable(MiniHexDungeon dungeon, CharacterOverworld cow)
        {
            //VERIFY failed remake actions after interact with dungeon while party not ready
            if (dungeon.IsDungeonCleared()) return false;
            List<FTKPlayerID> readyPlayers = dungeon.GetLoadPartyPlayers(cow, GameFlow.CombatType.Fight);
            int num = 0;
            foreach (CharacterOverworld _cow in FTKHub.Instance.m_CharacterOverworlds)
            {
                if (!readyPlayers.Contains(_cow.m_FTKPlayerID))
                {
                    if (!GameFlow.Instance.IsPermaDeath || !_cow.m_WaitForRespawn)
                    {
                        num++;
                    }
                }
            }
            if (num != 0 && cow.GetHexLand() == dungeon.m_HexLand)
            {
                if (dungeon.m_ID != FTK_dungeonEncounter.ID.Harazuel)
                {
                    Context.Send("your entire party needs to be alive and within range to enter the dungeon", true);
                    return false;
                }
            }
            return true;
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
            if (cowOnLand && hex.IsShoreWater() && hex.IsBoat()) return true;
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
                hasDeadPlayers = "has dead character to revive.";
            }
            MiniHexInfo hexInfo = hex.GetPOI();
            if (hexInfo != null)
            {
                MiniHexInfo.MiniHexType poiType = hexInfo.m_MiniHexType;
                string type = GetHexTypeContext(poiType);
                poi = hexInfo.GetPOIDisplayValue() + $"{type} ";
                if (IsPoiCompleted(hexInfo, cow))
                {
                    poi += " (completed)";
                }
            }
            if (addToList) OverworldFlow.hexPositions.Add(pos.ToString(), hex);
            return $"[{pos} ({name})({questName}){hasDeadPlayers}{poi}]";
        }

        public static string GetHexTypeContext(MiniHexInfo.MiniHexType type)
        {
            return type switch
            {
                MiniHexInfo.MiniHexType.Haunt => "(important to defeat)",
                MiniHexInfo.MiniHexType.Poison or MiniHexInfo.MiniHexType.Chaos or MiniHexInfo.MiniHexType.Fire or MiniHexInfo.MiniHexType.Curse => "(dangerous, avoid)",
                MiniHexInfo.MiniHexType.Portal => "(teleport)",
                _ => "",
            };
        }
    }
}