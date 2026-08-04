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
        public static ActionWindow window;
        public static bool isSearching = false;
        public static bool isTracking = false;
        public static bool isFirstAction = false;
        public static bool isSneakMovement = false;
        public static List<HexLand> tiles = [];
        public static readonly Dictionary<string, QuestLogicBase> questDict = [];
        public static readonly Dictionary<string, HexLand> hexPositions = [];

        static readonly bool removeEmptyWater = false;
        static bool isRemake = false;
        static readonly List<Vector3> questPositions = [];
        static StringBuilder sbQuest = new();
        static readonly Dictionary<CharacterOverworld, HexLand> lastDestinations = [];


        [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.InitializeSkipTurn))]
        [HarmonyPostfix]
        static void TurnSkipped(CharacterOverworld _cow)
        {
            Context.Send($"{CharacterData.GetCharacterName(_cow)} had their turn skipped");
            // DisposeActions();
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.OnStartTurn))] // before begin turn enumerate finished
        [HarmonyPostfix]
        static void StartTurn(CharacterOverworld __instance)
        {
            // Plugin.Logger.LogWarning("CharacterOverworld.OnStartTurn");
            // BeginTurn2(__instance);
        }

        [HarmonyPatch(typeof(CharacterOverworld), "BeginTurnTransition")]
        [HarmonyPostfix]
        static IEnumerator BeginTurn(IEnumerator __result, bool _isLoadGame, CharacterOverworld __instance)
        {
            GlobalConfig.GameLoaded();
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
            Plugin.Logger.LogMessage("start tracking");
            ResumeTurnMovement();
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
            StopTracking();
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
            if (isRemake) return;
            isRemake = true;
            Plugin.Logger.LogMessage("movement focus added");
            DisposeActions();
            tiles.Clear();
            QuickTimerCallback timer = new(() => GetValidMoveTiles(RollSystem.currentCOW), RollSystem.currentCOW.gameObject, 0.5f);
        }

#region Reverse Patches

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

#endregion

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
        static void OnTeleported(HexLand _newLand, CharacterOverworld __instance)
        {
            Context.Send($"{CharacterData.GetCharacterName(__instance)} teleported to {HexData.GetVec2Pos(_newLand)}");
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

        static void BeginTurn2(CharacterOverworld cow, bool registerBelt = true)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld || cow.IsInDungeon() || cow.m_CharacterStats.m_IsInCombat) return;
            if (cow.m_FirstStopAtHex) // is this useful
            {
                Plugin.Logger.LogWarning("first stop");
            }
            if (cow.IsInBoat() && GameLogic.Instance.GetActivePlayersInAreaOnLand(cow.GetHexLand(), GameFlow.Instance.m_PartyEnterRadius).Count > 0)
            {
                Plugin.Logger.LogWarning("boat players in range should gen location menu");
                isFirstAction = false;
                return;
            }
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
            ToggleDisposableActions.ToggleOverworldActions(true, false);
        }

        static void ResumeTurnMovement()
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            CharacterOverworld cow = CharacterData.GetNeuroCow();
            if (!Multiplayer.IsYourCow(cow)) return;
            isTracking = true;
            if (isFirstAction) return;
            RollSystem.currentCOW = cow;
            Plugin.Logger.LogMessage("start tracking create window");
            QuickTimerCallback timer = new(() => GetValidMoveTiles(cow), Movement.Instance.m_CursorHexRenderer.gameObject, 0.5f);
        }

        public static IEnumerator MoveToHexCoroutine(CharacterOverworld curCow, HexLand hex, bool outOfRange = false, bool isSameHex = false)
        {
            HexLand dest = hex;
            if (!isSameHex)
            {
                lastDestinations[curCow] = dest;
            }
            // hover destination to generate path list
            ReverseClearDrawPath(Movement.Instance, Movement.Instance.m_HexListPartial);
            ReverseCheckHoverPath(Movement.Instance, dest);
            List<HexLand> hexes = [.. Movement.Instance.m_HexListPartial];
            if (!isTracking || GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
                Plugin.Logger.LogError("tried to execute move action while character is not in tracking state");
                Context.Send(StringMessages.ActionIssueOccured.Format(["movement"]) + NeuroSdkStrings.ModFaultSuffix, true);
                CreateActionWindow(curCow);
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
                    if (HexData.CanTravel(dest, curCow))
                    {
                        dest = hexes[i];
                        failed = false;
                        break;
                    }
                    Plugin.Logger.LogWarning("cant auto travel to last hex " + i);
                    isSameTarget = false;
                    if (i == 0)
                    {
                        Plugin.Logger.LogError("could not find any valid hexes");
                        Context.Send("could not find any valid hexes for the movement action", true);
                        CreateActionWindow(curCow);
                        yield break;
                    }
                }
                if (failed)
                {
                    Plugin.Logger.LogError("failed to auto travel to last hex");
                    Context.Send(StringMessages.ActionIssueOccured.Format(["go_to_quest"]), true);
                    CreateActionWindow(curCow);
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.1f);
            string ctx = $"moving to {HexData.GetContextForHex(curCow, dest)}";
            if (!isSameTarget) ctx += " (could not reach your chosen destination)";
            if (isSameHex) ctx = "interacting with this hexes point of interest";
            else if (dest == curCow.GetHexLand())
            {
                Plugin.Logger.LogError($"target destination was same hex: {dest.GetPosition()} = {curCow.GetHexLand().GetPosition()}");
                Context.Send($"your final path destination is the hex you are currently on, choose a different action, option or end turn to stay here", true);
                CreateActionWindow(curCow);
                yield break;
            }
            Context.Send(ctx, true);
            ReverseClearDrawPath(Movement.Instance, Movement.Instance.m_HexListPartial);
            ReverseCheckHoverPath(Movement.Instance, dest); // make sure hex list is up to date
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
            CharacterOverworld currentCOW = CharacterData.GetNeuroCow();
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
            if (removeEmptyWater)
            {
                bool removed = false;
                List<HexLand> toRemove = [];
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i].IsWater())
                    {
                        toRemove.Add(tiles[i]);
                        removed = true;
                    }
                }
                foreach (HexLand hex in toRemove) tiles.Remove(hex); // VERIFY removed empty water hex
                if (removed) Plugin.Logger.LogWarning($"removed empty water tiles");
            }
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
            CharacterOverworld cow = CharacterData.GetNeuroCow();
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
            Vector3 cowHex = CharacterData.GetNeuroCow().GetHexLand().GetPosition();
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

        #endregion



        public static void CreateActionWindow(CharacterOverworld _cow)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            HexLand hex = _cow.GetHexLand();
            Vector2 pos = HexData.GetVec2Pos(hex);
            string ctx = $"you are controlling {CharacterData.GetCharacterName(_cow)} at hex {pos}.";
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
            IEnumerable<CharacterOverworld> validCows = CharacterData.GetCowsNotOnThisHex(_cow);
            window = MovementAction.CreateWindow(_cow, tileCtx, hexPositions, questDict, validQuests, validCows, HexData.IsPoiCompleted(hex.GetPOI(), _cow));
            isSearching = false;
        }

        public static void NeuroTryInteractWithHex(CharacterOverworld cow)
        {
            HexLand hex = cow.GetHexLand();
            cow.StartCoroutine(MoveToHexCoroutine(cow, hex, false, true));
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

        internal static void NeuroTryGoToCharacter(CharacterOverworld target)
        {
            CharacterOverworld curCow = CharacterData.GetNeuroCow();
            if (target == null)
            {
                Plugin.Logger.LogError("invalid cow");
                Context.Send($"{StringMessages.ActionIssueOccured.Format(["go_to_character"]) + NeuroSdkStrings.ModFaultSuffix}", true);
                QuickTimerCallback timer = new(() => CreateActionWindow(curCow), curCow.gameObject, 2.0f);
                return;
            }
            HexLand dest = target.GetHexLand();
            curCow.StartCoroutine(MoveToHexCoroutine(curCow, dest, true));
        }

        static bool IsQuestAcrossWater(HexLand lastTarget)
        {
            if (lastTarget.IsShoreLand()) return true;
            return false;
        }
    }
}


// // when the characters actions points change. This occurs with each tile passed
// [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]

// [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.Initialize))]
