using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class OverworldMovement
    {
        static ActionWindow window;
        static CharacterOverworld current;
        static int rollCount;
        static bool isSearching = false;
        static bool hasChoices = false;

        public static List<HexLand> tiles = [];
        public static bool isTracking = false;

        // enumerator to show the rolled values
        [HarmonyPatch(typeof(SlotControl), "DisplaySlots")]
        [HarmonyPostfix]
        static IEnumerator SlotResults(IEnumerator __result, SlotControl __instance, CharacterOverworld _cow, string[] _slotResults, int _slotSuccess, string _goodSlot)
        {
            Plugin.Logger.LogWarning(_cow?.m_PlayerName);
            Plugin.Logger.LogWarning(string.Join(", ", [.. _slotResults.Select(x => x)])); // basequickness, basequickness, quickness, quickness, miss
            isSearching = false;
            while (__result.MoveNext()) yield return __result.Current;
            if (hasChoices)
            {
                // Plugin.Logger.LogMessage("displaySlots; already have choices");
                hasChoices = false;
                yield break;
            }
            if (_cow.m_FTKPlayerID.IsPlayer()) // FIXME also counts dungeon enemies
            {
                Context.Send($"[character roll result] {__instance.m_CurrentSuccess} / {_slotResults.Length}");
            }
            else
            {
                Plugin.Logger.LogMessage("is enemy " + _cow.m_FTKPlayerID.IsEnemy());
                Context.Send($"[enemy roll result] {__instance.m_CurrentSuccess} / {_slotResults.Length}");
            }
            switch (ToggleOverworldActions.mode)
            {
                case uiGameTrackerHUD.GameTrackerMode.Overworld:
                    current = GameLogic.Instance.GetCurrentCOW();
                    rollCount = current.m_CharacterStats.m_ActionPoints;
                    Plugin.Logger.LogMessage($"display slots finished; rolled {rollCount}");
                    GetValidMoveTiles(HexLand.SelectType.Land, current);
                    break;
                case uiGameTrackerHUD.GameTrackerMode.Combat:
                    // no effect needed as rolls are shown with animation
                    break;
            }
        }

        // when movement choice starts
        [HarmonyPatch(typeof(Movement), nameof(Movement.StartTracking))]
        [HarmonyPostfix]
        static void StartTracking()
        {
            isTracking = true;
            Plugin.Logger.LogWarning("START tracking");
            if (hasChoices)
            {
                Plugin.Logger.LogMessage("tracking; already have choices");
                return;
            }
            current = GameLogic.Instance.GetCurrentCOW();
            QuickTimerCallback timer = new(() => GetValidMoveTiles(HexLand.SelectType.Land, current), 1500f);
        }

        // when movement begins
        [HarmonyPatch(typeof(Movement), nameof(Movement.StopTracking))]
        [HarmonyPostfix]
        static void StopTracking()
        {
            isTracking = false;
            Plugin.Logger.LogWarning("STOP tracking");
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(CharacterOverworld), "BeginTurnTransition")]
        [HarmonyPostfix]
        static IEnumerator BeginTurn(IEnumerator __result, bool _isLoadGame)
        {
            GameDefinition gameDef = GameLogic.Instance.GetGameDef();
            Context.Send($"game round: {GameFlow.Instance.m_RoundCount}, stage percent: {FTKUtil.RoundToInt(gameDef.GetGameStage().GetStagePassedPercent() * 100f)}, stage progression: {gameDef.GetGameStage().GetCurrentProgressionTier()}, player progression: {FTK_progressionTierDB.GetDB().GetNaturalProgressionTierOfParty()}", true);
            while (__result.MoveNext()) yield return __result.Current;
            Plugin.Logger.LogMessage("begin turn transition finished");
            hasChoices = false;
            isTracking = false;
            isSearching = false;
        }

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            tiles.Clear();
            hasChoices = false;
            isTracking = false;
            isSearching = false;
        }

        // when the character stops moving
        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.OnStopAtHex))]
        [HarmonyPostfix]
        static void PlayerStopped(CharacterOverworld __instance)
        {
            hasChoices = false;
            Plugin.Logger.LogMessage("stop at hex");
        }

        // when the characters actions points change. This occurs with each tile passed
        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]
        [HarmonyPostfix]
        static void UpdatePlayerAction(CharacterOverworld __instance)
        {
            // Plugin.Logger.LogMessage("update player action");
        }

        // spending focus for more actions
        [HarmonyPatch(typeof(Movement), "ConvertFocusToAction")]
        [HarmonyPostfix]
        static void OnFocusAction()
        {
            if (rollCount == current.m_CharacterStats.m_ActionPoints) return; // no change
            Plugin.Logger.LogMessage("movement focus added");
            GetValidMoveTiles(HexLand.SelectType.Land, current);
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

        static void GetValidMoveTiles(HexLand.SelectType type, MonoBehaviour routineOwner)
        {
            if (!isTracking)
            {
                Plugin.Logger.LogError("not tracking");
                return;
            }
            if (!routineOwner.isActiveAndEnabled)
            {
                Plugin.Logger.LogError("routine owner is disabled");
                return;
            }
            routineOwner.StartCoroutine(GetValidTiles(type));
        }

        static IEnumerator GetValidTiles(HexLand.SelectType type = HexLand.SelectType.Same)
        {
            if (isSearching) yield break;
            isSearching = true;
            hasChoices = false;
            tiles.Clear();
            Object.Destroy(window);
            CharacterOverworld currentCOW = GameLogic.Instance.GetCurrentCOW();
            int points = currentCOW.m_CharacterStats.m_ActionPoints;
            rollCount = points;
            double startTime = Time.time;
            Task task = Task.Factory.StartNew(() => tiles = [.. LoopNeighbors(currentCOW, points)]);
            yield return task.IsCompleted;
            Plugin.Logger.LogWarning($"found {tiles.Count} tiles: {Time.time - startTime} seconds");
            current.StartCoroutine(Wait());

            static IEnumerator Wait()
            {
                yield return new WaitForSeconds(0.5f);
                window = MovementAction.RegisterAction(current.gameObject, tiles);
                isSearching = false;
            }
        }

        static List<HexLand> LoopNeighbors(CharacterOverworld owner, int points)
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
            hasChoices = true;
            return validNeighbors;
        }
    }
}

