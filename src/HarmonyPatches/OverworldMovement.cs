using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class OverworldMovement
    {
        static ActionWindow window;
        public static bool isSearching = false;
        public static bool isTracking = false;
        public static List<HexLand> tiles = [];

        public static readonly Dictionary<string, QuestLogicBase> questDict = [];
        static readonly List<Vector3> questPositions = [];
        static StringBuilder sbQuest = new();
        static readonly Dictionary<string, HexLand> hexPositions = [];


        [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.InitializeSkipTurn))]
        [HarmonyPostfix]
        static void TurnSkipped(CharacterOverworld _cow)
        {
            Context.Send($"{_cow.m_CharacterStats.m_CharacterName} had their turn skipped");
            Object.Destroy(window);
        }

        // when movement choice starts
        [HarmonyPatch(typeof(Movement), nameof(Movement.StartTracking))]
        [HarmonyPostfix]
        static void StartTracking()
        {
            Plugin.Logger.LogWarning("START tracking");
            ToggleOverworldActions.EnableOverworldActions();
            isTracking = true;
            RollSystem.currentCOW = GameLogic.Instance.GetCurrentCOW();
            QuickTimerCallback timer = new(() => GetValidMoveTiles(RollSystem.currentCOW), Movement.Instance.m_CursorHexRenderer.gameObject);
        }

        // when movement begins
        [HarmonyPatch(typeof(Movement), nameof(Movement.StopTracking))]
        [HarmonyPostfix]
        static void StopTracking()
        {
            Plugin.Logger.LogWarning("STOP tracking");
            isTracking = false;
            isSearching = false;
            tiles.Clear();
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(CharacterOverworld), "BeginTurnTransition")]
        [HarmonyPostfix]
        static IEnumerator BeginTurn(IEnumerator __result, bool _isLoadGame)
        {
            BeginTurns.SendOverworldTurnBeginStats(GameLogic.Instance.GetCurrentCOW());
            GameDefinition gameDef = GameLogic.Instance.GetGameDef();
            Context.Send($"game round: {GameFlow.Instance.m_RoundCount}. stage percent: {FTKUtil.RoundToInt(gameDef.GetGameStage().GetStagePassedPercent() * 100f)}. stage progression: {gameDef.GetGameStage().GetCurrentProgressionTier()}. player progression: {FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfParty()}", true);
            isTracking = false;
            isSearching = false;
            while (__result.MoveNext()) yield return __result.Current;
            if (_isLoadGame)
            {
                QuickTimerCallback timer = new(() => GetValidMoveTiles(GameLogic.Instance.GetCurrentCOW()), Movement.Instance.m_CursorHexRenderer.gameObject);
            }
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            tiles.Clear();
            isTracking = false;
            isSearching = false;
            Object.Destroy(window);
        }

        // when the character stops moving
        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.OnStopAtHex))]
        [HarmonyPostfix]
        static void PlayerStopped(CharacterOverworld __instance)
        {
            Object.Destroy(window);
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
            ToggleOverworldActions.EnableOverworldActions();
        }

        [HarmonyPatch(typeof(CharacterSkills), nameof(CharacterSkills.Refocus))]
        [HarmonyPrefix]
        static void Refocus()
        {
            Plugin.Logger.LogWarning("NYI end turn refocus skill proc");//TODO
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
            currentCOW.StartCoroutine(Wait());

            static IEnumerator Wait()
            {
                yield return new WaitForSeconds(1f);
                CreateActionWindow();
                isSearching = false;
            }
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
            //1. if hex is land & cow on land => land=>land
            if (isLand && cowOnLand) return true;
            //2. if hex is land & cow on boat => boat=>land
            if (isLand && onBoat) return true;
            //3. if hex is water & cow on land => land=>water
            if (!isLand && cowOnLand) return false;
            //4. if hex is water & cow on boat => boat=>water
            if (!isLand && onBoat) return true;
            //5. if hex has boat & cow on land => land=>boat
            if (hex.IsBoat() && cowOnLand) return true;
            // what would 2 boats do
            // what about air
            return false;
        }


        /// <summary>
        /// display as [(position x,z) (name/realm)(quest name)other info]
        /// </summary>
        static string GetTileContext(List<HexLand> _tiles)
        {
            hexPositions.Clear();
            // [(155.8, 20.0): (The Guardian Forest)(). Woodsmoke]
            StringBuilder sb = new();
            sb.Append("[all tiles in range] (displayed as [(position x,z) (name/realm)(quest)other info]) ");
            string name;
            string questName;
            string hasDeadPlayers;
            string characters = "";
            string poi;
            Vector3 itemPos;
            Vector2 pos;
            // FTK_realm.ID realm;
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            foreach (HexLand hex in _tiles)
            {
                poi = "";
                hasDeadPlayers = "";
                questName = "";
                _ = characters;
                // realm = hex.GetRealm();
                // GuardianForest | GoldenPlains
                // Plugin.Logger.LogWarning("realm: " + realm);
                // distance = (float)Math.Round(HexLand.Distance(cow.m_HexLand, hex), 2);
                name = hex.GetLocationDisplayValue(cow);
                itemPos = hex.GetPosition();
                pos = new Vector2(itemPos.x, itemPos.z);
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
                sb.AppendLine($"[{pos} ({name})({questName}){hasDeadPlayers + ". "}{poi}]");
                hexPositions.Add(pos.ToString(), hex);
            }
            return sb.ToString();
        }


        #region quests

        static void GetQuestData()
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
            if (sbQuest.Length > 0) Context.Send(sbQuest.ToString());
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
                Vector3 pos1 = dest.GetPosition();
                Vector2 pos2 = new(pos1.x, pos1.z);
                if (questDict.ContainsKey(pos2.ToString())) return;
                questDict.Add(pos2.ToString(), quest);
                questPositions.Add(pos1);
                sbQuest.AppendLine($"[{type} quest at {pos2}]: {description}");
                // [Warning:Neuro For the King] quest desc: Kill the Chaos Leader in The Guardian Forest
                // [Warning:Neuro For the King] quest pos: (85.1, 117.5)
            }
        }

        static QuestLogicBase TileHasQuestObjective(HexLand hex)
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

        public static void CreateActionWindow()
        {
            GetQuestData();
            List<INeuroAction> actions = [];
            string ctx = GetTileContext(tiles);
            actions.Add(new MovementAction(hexPositions));
            if (GameLogic.Instance.GetCurrentCOW()?.GetHexLand()?.HasPOI() ?? false)
            {
                actions.Add(new InteractWithCurrentHex());
            }
            if (!GlobalConfig.debug_mode) actions.Add(new EndTurnAction());
            if (questDict.Count > 0)
            {
                actions.Add(new GoToHexAction(new Dictionary<string, QuestLogicBase>(questDict)));
            }
            window = MovementAction.CreateAction(GameLogic.Instance.GetCurrentCOW(), ctx, actions);
        }

    }
}


// // when the characters actions points change. This occurs with each tile passed
// [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]

// [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.Initialize))]
