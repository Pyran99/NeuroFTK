using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GridEditor;
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
        public static ActionWindow window;
        public static bool isSearching = false;
        public static bool isTracking = false;
        public static List<HexLand> tiles = [];

        public static readonly Dictionary<string, QuestLogicBase> questDict = [];
        static readonly List<Vector3> questPositions = [];
        static StringBuilder sbQuest = new();
        public static readonly Dictionary<string, HexLand> hexPositions = [];
        static readonly Dictionary<CharacterOverworld, HexLand> lastDestinations = []; 

        public static bool isFirstAction = false;
        public static bool isSneakMovement = false;


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
            while (__result.MoveNext()) yield return __result.Current;
            BeginTurn2(__instance);
            // GameDefinition gameDef = GameLogic.Instance.GetGameDef();
            // Context.Send($"game round: {GameFlow.Instance.m_RoundCount}. stage percent: {FTKUtil.RoundToInt(gameDef.GetGameStage().GetStagePassedPercent() * 100f)}. stage progression: {gameDef.GetGameStage().GetCurrentProgressionTier()}. player progression: {FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfParty()}", true);
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
            QuickTimerCallback timer = new(() => GetValidMoveTiles(cow), Movement.Instance.m_CursorHexRenderer.gameObject, 0.5f);
        }

        // when movement begins
        [HarmonyPatch(typeof(Movement), nameof(Movement.StopTracking))]
        [HarmonyPostfix]
        static void StopTracking()
        {
            isTracking = false;
            isSearching = false;
            isFirstAction = false;
            isSneakMovement = false;
            isRemake = false;
            tiles.Clear();
            DisposeActions();
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
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

        static bool isRemake = false;

        // spending focus for more actions
        [HarmonyPatch(typeof(Movement), "ConvertFocusToAction")]
        [HarmonyPostfix]
        static void OnFocusAction()
        {
            if (RollSystem.rollCount == RollSystem.currentCOW.m_CharacterStats.m_ActionPoints) return; // no change
            if (isRemake) return;
            isRemake = true;
            Plugin.Logger.LogMessage("movement focus added");
            DisposeActions();
            tiles.Clear();
            QuickTimerCallback timer = new(() => GetValidMoveTiles(RollSystem.currentCOW), RollSystem.currentCOW.gameObject, 0.5f);
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

        [HarmonyPatch(typeof(Movement), "UpdateHexMove")]
        [HarmonyReversePatch]
        public static void ReverseUpdateHexMove(object instance)
        {
        }

        [HarmonyPatch(typeof(Movement), "ClearDrawPath")]
        [HarmonyReversePatch]
        public static void ReverseClearDrawPath(object instance, List<HexLand> _path)
        {
        }

        [HarmonyPatch(typeof(MiniHexInfo), nameof(MiniHexInfo.DeactivateHex))]
        [HarmonyPostfix]
        static void HexDeactivated(MiniHexInfo __instance)
        {
            if (!GlobalConfig.gameInitialized) return;
            // skip types that dont matter
            if (!HexData.IsUsedDeactivateCtx(__instance.m_MiniHexType)) return;
            Context.Send($"{__instance.GetPOIDisplayValue()} at {HexData.GetVec2Pos(__instance.m_HexLand)} has been deactivated", true);
        }

        [HarmonyPatch(typeof(MiniHexAlluringPool), nameof(MiniHexAlluringPool.DeactivateHex))]
        [HarmonyPostfix]
        static void HexDeactivatedPool(MiniHexAlluringPool __instance)
        {
            if (!GlobalConfig.gameInitialized) return;
            Context.Send($"{__instance.GetPOIDisplayValue()} at {HexData.GetVec2Pos(__instance.m_HexLand)} has been deactivated", true);
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.PortalMoveTo))]
        [HarmonyPostfix]
        static void OnTeleported(HexLand _newLand)
        {
            Context.Send($"{CharacterData.GetCharacterName(GameLogic.Instance.GetCurrentCOW())} teleported to {HexData.GetVec2Pos(_newLand)}");
        }


        #region end turn procs

        [HarmonyPatch(typeof(CharacterSkills), nameof(CharacterSkills.Refocus))]
        [HarmonyPrefix]
        static void Refocus(ref bool __result)
        {
            if (__result) Context.Send("gained focus points from end of turn skill", true);
        }

        #endregion


        public static void BeginMovementTurn()
        {
            isFirstAction = false;
            StartTracking();
        }

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
            ReverseClearDrawPath(Movement.Instance, Movement.Instance.m_HexListPartial);
            ReverseCheckHoverPath(Movement.Instance, dest);
            List<HexLand> hexes = [.. Movement.Instance.m_HexListPartial];
            if (!isTracking || GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogError("tried to execute move action while character is not in tracking state");
                Context.Send(StringMessages.ActionIssueOccured.Format(["movement"]) + NeuroSdkStrings.ModFaultSuffix, true);
                CreateActionWindow(cow);
                yield break;
            }
            bool isSameTarget = hexes.Contains(hex);
            if (outOfRange && !isSameTarget && !isSameHex)
            {
                // the generated move path from hover
                dest = hexes.Last();
                bool failed = true;
                for (int i = hexes.Count-1; i >= 0; i--)
                {
                    if (HexData.CanTravel(dest, cow))
                    {
                        dest = hexes[i];
                        failed = false;
                        break;
                    }
                    Plugin.Logger.LogWarning("cant auto travel to last hex " + i);
                    isSameTarget = false;
                    if (i == 0)
                    {
                        Plugin.Logger.LogError("could not find any valid tiles");
                        Context.Send("could not find any valid tiles for the movement action", true);
                        CreateActionWindow(cow);
                        yield break;
                    }
                }
                if (failed)
                {
                    Plugin.Logger.LogError("failed to auto travel to last hex");
                    Context.Send(StringMessages.ActionIssueOccured.Format(["go_to_quest"]), true);
                    CreateActionWindow(cow);
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.1f);
            ReverseClearDrawPath(Movement.Instance, Movement.Instance.m_HexListPartial);
            ReverseCheckHoverPath(Movement.Instance, dest); // make sure hex list is up to date
            string ctx = $"moving to {HexData.GetContextForHex(cow, dest)}";
            if (!isSameTarget) ctx += " (could not reach your chosen destination)";
            if (isSameHex) ctx = "interacting with this tiles point of interest";
            Context.Send(ctx, true);
            ReverseCheckClickPath(Movement.Instance, dest, true, false, false);
        }

        public static void GetValidMoveTiles(MonoBehaviour routineOwner, HexLand.SelectType type = HexLand.SelectType.Same)
        {
            isRemake = false;
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
            if (!Multiplayer.IsYourCow(currentCOW))
            {
                Plugin.Logger.LogError("tried to generate move action from another players cow");
                yield break;
            }
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
                        if (neighbor.CanTravel() && HexData.CanTravel(neighbor, owner))
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
                sb.AppendLine(HexData.GetContextForHex(cow, hex, true));
            }
            return sb.ToString();
        }


        #region quests

        public static string GetQuestData()
        {
            questDict.Clear();
            questPositions.Clear();
            sbQuest = new();
            Vector3 cowHex = GameLogic.Instance.GetCurrentCOW().GetHexLand().GetPosition();
            foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>())
            {
                AddValidQuests(q, cowHex);
            }
            foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren<uiQuestItem>())
            {
                AddValidQuests(q, cowHex);
            }
            return sbQuest.ToString();
        }

        static void AddValidQuests(uiQuestItem questItem, Vector3 cowHex)
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
                if (dest.GetPosition() == cowHex)
                {
                    questDict.Add(pos.ToString(), quest);
                    questPositions.Add(dest.GetPosition());
                    sbQuest.AppendLine($"[{type} quest at {pos}]: {description} (you are currently at this hex)");
                    return;
                }
                string outOfRange = "";
                if ((dest.GetPosition() - cowHex).magnitude > 2.8866f * 15f)
                {
                    outOfRange = " (out of pathfinding range)";
                }
                questDict.Add(pos.ToString(), quest);
                questPositions.Add(dest.GetPosition());
                sbQuest.AppendLine($"[{type} quest at {pos}]: {description}{outOfRange}");
                // [Warning:Neuro For the King] quest desc: Kill the Chaos Leader in The Guardian Forest
                // [Warning:Neuro For the King] quest pos: (85.1, 117.5)
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

        #endregion


        static void BeginTurn2(CharacterOverworld cow, bool registerBelt = true)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld || cow.IsInDungeon() || cow.m_CharacterStats.m_IsInCombat) return;
            if (cow.m_WaitForRespawn || !cow.IsStillAlive())
            {
                Context.Send($"{CharacterData.GetCharacterName(cow)} is dead. they can choose to revive themself or wait for another character to revive them.");
                return;
            }
            if (!Multiplayer.IsYourCow(cow))
            {
                Multiplayer.SendOtherPlayerTurnCtx();
                return;
            }
            isFirstAction = true;
            isSearching = false;
            DisposeActions();
            QuickTimerCallback timer = new(() => window = MovementAction.CreateTurnBeginWindow(registerBelt), cow.gameObject);
            ToggleDisposableActions.ToggleOverworldActions(true);
        }

        public static void CreateActionWindow(CharacterOverworld _cow)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            HexLand hex = _cow.GetHexLand();
            Vector2 pos = HexData.GetVec2Pos(hex);
            string ctx = $"it is your turn, you are controlling {CharacterData.GetCharacterName(_cow)} at hex {pos}.";
            if (lastDestinations.ContainsKey(_cow))
            {
                if (lastDestinations[_cow] != null && lastDestinations[_cow] != hex)
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
            List<string> validQuests = GetInRangeQuests(_cow);
            window = MovementAction.CreateWindow(_cow, tileCtx, hexPositions, questDict, validQuests, IsHexInteractable(hex.GetPOI(), _cow));
            isSearching = false;
        }

        static List<string> GetInRangeQuests(CharacterOverworld cow)
        {
            List<string> result = [];
            List<Vector3> positions = GetQuestPositions();
            foreach (KeyValuePair<string, QuestLogicBase> kvp in questDict)
            {
                Vector3 dest = kvp.Value.GetHexLandDestination()?.GetPosition() ?? Vector3.positiveInfinity;
                if (dest == cow.GetHexLand().GetPosition()) continue;
                if (positions.Contains(dest))
                {
                    if ((dest - cow.GetHexLand().GetPosition()).magnitude < 2.8866f * 15f)
                    {
                        result.Add(kvp.Key);
                    }
                }
            }
            return result;
        }

        public static void NeuroTryGoToQuest(CharacterOverworld cow, QuestLogicBase quest)
        {
            if (quest == null)
            {
                Plugin.Logger.LogError("chosen quest was null");
                Context.Send($"{StringMessages.ActionIssueOccured.Format(["go_to_quest"]) + NeuroSdkStrings.ModFaultSuffix}", true);
                QuickTimerCallback timer = new(() => CreateActionWindow(cow), cow.gameObject, 2.0f);
                return;
            }
            HexLand dest = quest.GetHexLandDestination();
            cow.StartCoroutine(MoveToHexCoroutine(cow, dest, true));
        }

        public static void NeuroTryInteractWithHex(CharacterOverworld cow)
        {
            HexLand hex = cow.GetHexLand();
            cow.StartCoroutine(MoveToHexCoroutine(cow, hex, false, true));
        }

        static bool IsHexInteractable(MiniHexInfo poi, CharacterOverworld cow)
        {
            if (HexData.IsPoiComplete(poi)) return false;
            Plugin.Logger.LogMessage("poi type = " + poi.m_MiniHexType);
            bool interactable = true;
            switch (poi.m_MiniHexType)
            {
                case MiniHexInfo.MiniHexType.Town:
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
            if (!interactable)
            {
                Plugin.Logger.LogMessage("this hex is not interactable");
                // QuickTimerCallback timer = new(() => CreateActionWindow(cow), cow.gameObject, 0.5f);
            }
            return interactable;
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
            if (encounter.m_Type == FTK_miniEncounter.ID.kvHome)
            {
                Context.Send($"{CharacterData.GetCharacterName(cow)} does not have the required quest item for this hex", true);
                return false;
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
            if (num != 0)
            {
                if (dungeon.m_ID != FTK_dungeonEncounter.ID.Harazuel)
                {
                    Context.Send("your entire party needs to be alive and within range to enter the dungeon", true);
                    return false;
                }
            }
            return true;
        }

    }
}


// // when the characters actions points change. This occurs with each tile passed
// [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]

// [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.Initialize))]
