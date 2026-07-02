using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
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


        [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.InitializeSkipTurn))]
        [HarmonyPostfix]
        static void TurnSkipped(CharacterOverworld _cow)
        {
            Context.Send($"{_cow.m_CharacterStats.m_CharacterName} had their turn skipped");
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
            QuickTimerCallback timer = new(() => GetValidMoveTiles(RollSystem.currentCOW));
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
            GameDefinition gameDef = GameLogic.Instance.GetGameDef();
            Context.Send($"game round: {GameFlow.Instance.m_RoundCount}, stage percent: {FTKUtil.RoundToInt(gameDef.GetGameStage().GetStagePassedPercent() * 100f)}, stage progression: {gameDef.GetGameStage().GetCurrentProgressionTier()}, player progression: {FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfParty()}", true);
            isTracking = false;
            isSearching = false;
            while (__result.MoveNext()) yield return __result.Current;
            if (_isLoadGame)
            {
                QuickTimerCallback timer = new(() => GetValidMoveTiles(GameLogic.Instance.GetCurrentCOW()));
            }
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            tiles.Clear();
            isTracking = false;
            isSearching = false;
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

        [HarmonyPatch(typeof(EncounterSessionMC), "ReturnToOverworld")]
        [HarmonyPostfix]
        static void ReturnedToOverworld()
        {
            ToggleOverworldActions.EnableOverworldActions();
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
                yield return new WaitForSeconds(0.25f);
                window = MovementAction.RegisterAction(RollSystem.currentCOW.gameObject, tiles);
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
                        //TODO match types
                        if (neighbor.CanTravel() && !neighbor.IsWater())
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
    }
}


// // when the characters actions points change. This occurs with each tile passed
// [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]

// [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.Initialize))]
