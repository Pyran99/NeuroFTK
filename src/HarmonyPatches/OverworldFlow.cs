using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class OverworldFlow
    {
        static ActionWindow window;
        public static bool isSearching = false;
        public static bool isTracking = false;
        public static List<HexLand> tiles = [];

        public static readonly Dictionary<string, QuestLogicBase> questDict = [];
        static readonly List<Vector3> questPositions = [];
        static StringBuilder sbQuest = new();
        public static readonly Dictionary<string, HexLand> hexPositions = [];
        static readonly Dictionary<CharacterOverworld, HexLand> lastDestinations = []; 

        public static bool isFirstAction = false;


        [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.InitializeSkipTurn))]
        [HarmonyPostfix]
        static void TurnSkipped(CharacterOverworld _cow)
        {
            Context.Send($"{CharacterData.GetCharacterName(_cow)} had their turn skipped");
            DisposeActions();
        }

        [HarmonyPatch(typeof(CharacterOverworld), "BeginTurnTransition")] // not called when loading game
        [HarmonyPostfix]
        static IEnumerator BeginTurn(IEnumerator __result, bool _isLoadGame, CharacterOverworld __instance)
        {
            GlobalConfig.gameInitialized = true;
            isFirstAction = true;
            isSearching = false;
            Object.Destroy(window);
            while (__result.MoveNext()) yield return __result.Current;
            BeginTurn2(__instance);
            // GameDefinition gameDef = GameLogic.Instance.GetGameDef();
            // Context.Send($"game round: {GameFlow.Instance.m_RoundCount}. stage percent: {FTKUtil.RoundToInt(gameDef.GetGameStage().GetStagePassedPercent() * 100f)}. stage progression: {gameDef.GetGameStage().GetCurrentProgressionTier()}. player progression: {FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfParty()}", true);
        }

        static void BeginTurn2(CharacterOverworld cow)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld || cow.IsInDungeon() || cow.m_CharacterStats.m_IsInCombat) return;
            if (!Multiplayer.IsYourCow(cow))
            {
                Multiplayer.SendOtherPlayerTurnCtx();
                return;
            }
            isFirstAction = true;
            isSearching = false;
            Object.Destroy(window);
            QuickTimerCallback timer = new(() => window = MovementAction.CreateTurnBeginWindow(), cow.gameObject);
            ToggleDisposableActions.ToggleOverworldActions(true, true);
        }

        // when movement choice starts
        [HarmonyPatch(typeof(Movement), nameof(Movement.StartTracking))]
        [HarmonyPostfix]
        public static void StartTracking()
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            if (!Multiplayer.IsYourCow(cow)) return;
            isTracking = true;
            if (isFirstAction) return;
            RollSystem.currentCOW = cow;
            Plugin.Logger.LogMessage("start tracking create window");
            QuickTimerCallback timer = new(() => GetValidMoveTiles(cow), Movement.Instance.m_CursorHexRenderer.gameObject);
        }

        // when movement begins
        [HarmonyPatch(typeof(Movement), nameof(Movement.StopTracking))]
        [HarmonyPostfix]
        static void StopTracking()
        {
            isTracking = false;
            isSearching = false;
            isFirstAction = false;
            tiles.Clear();
            DisposeActions();
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            ToggleDisposableActions.ToggleOverworldActions(false);
            tiles.Clear();
            isTracking = false;
            isSearching = false;
            DisposeActions();
        }

        // when the character stops moving
        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.OnStopAtHex))]
        [HarmonyPostfix]
        static void PlayerStopped(CharacterOverworld __instance)
        {
            
        }

        // spending focus for more actions
        [HarmonyPatch(typeof(Movement), "ConvertFocusToAction")]
        [HarmonyPostfix]
        static void OnFocusAction()
        {
            if (RollSystem.rollCount == RollSystem.currentCOW.m_CharacterStats.m_ActionPoints) return; // no change
            Plugin.Logger.LogMessage("movement focus added");
            Object.Destroy(window);
            tiles.Clear();
            Plugin.Logger.LogWarning("convert create window");
            GetValidMoveTiles(RollSystem.currentCOW);
        }

        // manual movement call
        [HarmonyPatch(typeof(Movement), "TrackCheckClickPath")]
        [HarmonyReversePatch]
        public static void ReverseCheckClickPath(object instance, HexLand _hexland, bool _forceMove, bool _rightClick, bool _isController)
        {
        }

        // manual movement call
        [HarmonyPatch(typeof(Movement), "TrackCheckHoverPath")]
        [HarmonyReversePatch]
        public static void ReverseCheckHoverPath(object instance, HexLand _hexland)
        {
        }

        [HarmonyPatch(typeof(EncounterSessionMC), "ReturnToOverworld")]
        [HarmonyPostfix]
        static void ReturnedToOverworld()
        {
        }

        #region end turn procs

        [HarmonyPatch(typeof(CharacterSkills), nameof(CharacterSkills.Refocus))]
        [HarmonyPrefix]
        static void Refocus()
        {
            Plugin.Logger.LogWarning("NYI end turn refocus skill proc");
        }

        #endregion

        static void DisposeActions()
        {
            Object.Destroy(window);
        }

        public static IEnumerator MoveToHexCoroutine(CharacterOverworld cow, HexLand hex, bool outOfRange = false, bool isSameHex = false)
        {
            HexLand dest = hex;
            if (!isSameHex)
            {
                lastDestinations[cow] = dest;
            }
            // hover destination to generate path list
            ReverseCheckHoverPath(Movement.Instance, dest);
            if (!isTracking || GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogError("tried to execute move action while character is not in tracking state");
                Context.Send(StringMessages.ActionIssueOccured.Format(["movement"]) + NeuroSdkStrings.ModFaultSuffix, true);
                CreateActionWindow(cow);
                yield break;
            }
            bool isSameTarget = Movement.Instance.m_HexListPartial.Contains(hex);
            if (outOfRange)
            {
                // the generated move path from hover
                dest = Movement.Instance.m_HexListPartial.Last();
                bool failed = true;
                for (int i = Movement.Instance.m_HexListPartial.Count-1; i >= 0; i--)
                {
                    if (CanTravel(dest, cow))
                    {
                        dest = Movement.Instance.m_HexListPartial[i];
                        failed = false;
                        break;
                    }
                    Plugin.Logger.LogWarning("cant auto travel to last hex " + i);
                    isSameTarget = false;
                    if (i == 0)
                    {
                        Plugin.Logger.LogError("could not find any valid tiles");
                        Context.Send("could not find any valid tiles for the movement action", true);
                        yield break;
                    }
                }
                if (failed)
                {
                    Plugin.Logger.LogError("failed to auto travel to last hex");
                    Context.Send(StringMessages.ActionIssueOccured.Format(["go_to_quest"]), true);
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
            string ctx = $"moving to {GetContextForHex(cow, dest)}";
            if (!isSameTarget) ctx += " (could not reach your chosen destination)";
            if (isSameHex) ctx = "interacting with this tiles point of interest";
            Context.Send(ctx, true);
            ReverseCheckClickPath(Movement.Instance, dest, false, false, false);
        }

        public static void GetValidMoveTiles(MonoBehaviour routineOwner, HexLand.SelectType type = HexLand.SelectType.Same)
        {
            if (!isTracking) return;
            if (!routineOwner.isActiveAndEnabled)
            {
                Plugin.Logger.LogError("routine owner is disabled");
                return;
            }
            if (tiles.Count > 0) return;
            routineOwner.StartCoroutine(GetValidTiles(type));
        }

        static IEnumerator GetValidTiles(HexLand.SelectType type = HexLand.SelectType.Same)
        {
            if (isFirstAction) yield break;
            if (isSearching) yield break;
            isSearching = true;
            tiles.Clear();
            Object.Destroy(window);
            CharacterOverworld currentCOW = GameLogic.Instance.GetCurrentCOW();
            int points = currentCOW.m_CharacterStats.m_ActionPoints;
            RollSystem.rollCount = points;
            double startTime = Time.time;
            Task task = Task.Factory.StartNew(() => tiles = [.. LoopNeighbors(currentCOW, points)]);
            yield return task.IsCompleted;

            Plugin.Logger.LogWarning($"found {tiles.Count} tiles: {Time.time - startTime} seconds");
            QuickTimerCallback timer = new(() => CreateActionWindow(currentCOW), currentCOW.gameObject, 0.5f);
        }

        static List<HexLand> LoopNeighbors(CharacterOverworld owner, int points, HexLand.SelectType type = HexLand.SelectType.Same)
        {
            HexLand initialHex = owner.GetHexLand();
            List<HexLand> validNeighbors = [];
            List<HexLand> hasChecked = [];
            List<HexLand> currentLoop = [initialHex];
            List<HexLand> nextLoop = [];
            int loopCount = 0;
            while (loopCount < points)
            {
                foreach (HexLand item in currentLoop)
                {
                    List<HexLand> neighbors = [.. item.m_Neighbors];
                    foreach (HexLand neighbor in neighbors)
                    {
                        if (neighbor == initialHex) continue;
                        if (hasChecked.Contains(neighbor)) continue;
                        hasChecked.Add(neighbor);
                        if (neighbor.CanTravel() && CanTravel(neighbor, owner))
                        {
                            validNeighbors.Add(neighbor);
                            nextLoop.Add(neighbor);
                        }
                    }
                }
                currentLoop = nextLoop;
                nextLoop = [];
                loopCount++;
            }
            if (validNeighbors.Count == 0) Plugin.Logger.LogError("no valid neighbors found");
            return validNeighbors;
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


        /// <summary>
        /// display as [(position x,z) (name/realm)(quest name)other info]
        /// </summary>
        public static string GetTileContext(List<HexLand> _tiles)
        {
            hexPositions.Clear();
            StringBuilder sb = new();
            sb.Append(StringMessages.HexContext);
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            foreach (HexLand hex in _tiles)
            {
                sb.AppendLine(GetContextForHex(cow, hex, true));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addToList">adds to member hexPositions</param>
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
            Vector2 pos = HexData.GetVec2Pos(hex);
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
            if (hex.GetPOI() != null)
            {
                poi = hex.GetPOI().GetPOIDisplayValue();
            }
            if (addToList) hexPositions.Add(pos.ToString(), hex);
            return $"[{pos} ({name})({questName}){hasDeadPlayers + ". "}{poi}]";
        }


        #region quests

        public static string GetQuestData()
        {
            questDict.Clear();
            questPositions.Clear();
            sbQuest = new();
            foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>())
            {
                AddValidQuests(q);
            }
            foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren<uiQuestItem>())
            {
                AddValidQuests(q);
            }
            return sbQuest.ToString();
        }

        static void AddValidQuests(uiQuestItem questItem)
        {
            if (StringReplace.RemoveStyling(questItem.m_Display.text) == "??????") return;
            QuestLogicBase quest = questItem.m_Quest;
            if (quest == null) return;
            if (quest.IsConsiderComplete()) return;
            string type = "side";
            if (quest.HasQuestDefID()) // considered Bounty Story Quest
            {
                type = "story";
            }
            string description = StringReplace.RemoveStyling(quest.GetLocalizedOneLineDesc());
            HexLand dest;
            dest = quest.GetHexLandDestination();
            if (dest != null)
            {
                Vector2 pos = HexData.GetVec2Pos(dest);
                if (questDict.ContainsKey(pos.ToString())) return;
                questDict.Add(pos.ToString(), quest);
                questPositions.Add(dest.GetPosition());
                sbQuest.AppendLine($"[{type} quest at {pos}]: {description}");
                // [Warning:Neuro For the King] quest desc: Kill the Chaos Leader in The Guardian Forest
                // [Warning:Neuro For the King] quest pos: (85.1, 117.5)
            }
        }

        public static QuestLogicBase TileHasQuestObjective(HexLand hex)
        {
            MiniHexInfo poi = hex.GetPOI();
            if (poi?.HasEncounterQuest() ?? false)
            {
                return poi.GetEncounterQuest();
            }
            if (questPositions.Contains(hex?.GetPosition() ?? Vector3.positiveInfinity))
            {
                return GameLogic.Instance.GetQuestByID(questPositions.IndexOf(hex.GetPosition()));
            }
            return null;
        }

        #endregion


        public static void CreateActionWindow(CharacterOverworld _cow)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            Vector2 pos = HexData.GetVec2Pos(_cow.GetHexLand());
            string ctx = $"it is your turn, you are controlling {CharacterData.GetCharacterName(_cow)} at hex {pos}.";
            if (lastDestinations.ContainsKey(_cow))
            {
                if (lastDestinations[_cow] != null && lastDestinations[_cow] != _cow.GetHexLand())
                {
                    pos = HexData.GetVec2Pos(lastDestinations[_cow]);
                    ctx += $" the last hex you tried to move to with this character was {pos}.";
                }
            }
            foreach (CharacterOverworld player in FTKHub.Instance.m_CharacterOverworlds)
            {
                if (player == _cow) continue;
                string revive = player.m_WaitForRespawn ? " (waiting for revive)" : "";
                pos = HexData.GetVec2Pos(player.GetHexLand());
                ctx += $" teammate {CharacterData.GetCharacterName(player)}{revive} is at hex {pos},";
            }
            Context.Send(ctx);
            string _quests = GetQuestData();
            if (_quests != "") Context.Send(_quests);
            string tileCtx = GetTileContext(tiles);
            window = MovementAction.CreateWindow(_cow, tileCtx, hexPositions, questDict);
            isSearching = false;
        }

    }
}


// // when the characters actions points change. This occurs with each tile passed
// [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]

// [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.Initialize))]
