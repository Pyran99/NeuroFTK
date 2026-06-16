using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    [HarmonyPatch]
    public class OverworldMovement
    {
        public static List<HexLand> tiles = [];
        static int rollCount;
        static ActionWindow window;
        static bool isFirst = true;
        static bool isRunning = false;
        static CharacterOverworld current;

        // enumerator to show the rolled values
        [HarmonyPatch(typeof(SlotControl), "DisplaySlots")]
        [HarmonyPostfix]
        static IEnumerator SlotResults(IEnumerator __result)
        {
            tiles.Clear();
            while (__result.MoveNext()) yield return __result.Current;
            isRunning = false;
            switch (ToggleOverworldActions.mode)
            {
                case uiGameTrackerHUD.GameTrackerMode.Overworld:
                    current = GameLogic.Instance.GetCurrentCOW();
                    rollCount = current.m_CharacterStats.m_ActionPoints;
                    Plugin.Logger.LogMessage($"rolled {rollCount}");
                    GetValidMoveTiles(HexLand.SelectType.Land, current);
                    break;
                case uiGameTrackerHUD.GameTrackerMode.Combat:
                    // no effect needed as rolls are shown with animation
                    Plugin.Logger.LogWarning("combat slot display");
                    break;
            }
            isFirst = false;
        }

        // when the character stops moving
        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.OnStopAtHex))]
        [HarmonyPostfix]
        static void PlayerStopped(CharacterOverworld __instance)
        {
            // TODO need to check if stopped at something (town)
            // uiLocationMenuDisplay | uiLocationMenuEntry | ServiceButton
            if (uiStartGame.Instance.m_IsResuming && isFirst)
            {
                Plugin.Logger.LogWarning("resume game isFirst");
                isFirst = false;
                current = __instance;
                rollCount = __instance.m_CharacterStats.m_ActionPoints;
                if (rollCount == 0) return;
                GetValidMoveTiles(HexLand.SelectType.Land, current);
                return;
            }
            if (current != __instance)
            {
                current = __instance;
                Plugin.Logger.LogMessage("new character: stopped");
                return;
            }
            if (isFirst) return;
            rollCount = __instance.m_CharacterStats.m_ActionPoints;
            Plugin.Logger.LogMessage($"player stopped: rolls: {rollCount}");
            if (rollCount > 0)
            {
                GetValidMoveTiles(HexLand.SelectType.Land, current);
            }
        }

        // when the characters actions points change. This occurs with each tile passed
        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]
        [HarmonyPostfix]
        static void UpdatePlayerAction(CharacterOverworld __instance)
        {
            if (current != __instance)
            {
                current = __instance;
                Plugin.Logger.LogMessage("new character: update"); //TODO this is called before stop when new character selected
                return;
            }
            if (isFirst) return;
            if (__instance.m_IsMoving) return;
            if (rollCount == current.m_CharacterStats.m_ActionPoints)
            {
                Plugin.Logger.LogMessage("no change: update");
                return;
            }
            Plugin.Logger.LogWarning("update action points " + rollCount);
            GetValidMoveTiles(HexLand.SelectType.Land, current);
        }

        // spending focus for more actions
        [HarmonyPatch(typeof(Movement), "ConvertFocusToAction")]
        [HarmonyPostfix]
        static void OnFocusAction()
        {
            if (rollCount == current.m_CharacterStats.m_ActionPoints)
            {
                Plugin.Logger.LogMessage("no change: focus");
                return;
            }
            Plugin.Logger.LogMessage($"movement focus added");
            GetValidMoveTiles(HexLand.SelectType.Land, current);
        }

        [HarmonyPatch(typeof(Movement), "TrackCheckClickPath")]
        [HarmonyReversePatch]
        public static void ReverseCheckClickPath(object instance, HexLand _hexland, bool _forceMove, bool _rightClick, bool _isController)
        {
        }

        /// <summary>
        /// need to find data for boat&air
        /// </summary>
        static void GetValidMoveTiles(HexLand.SelectType type, MonoBehaviour routineOwner)
        {
            // routineOwner.StartCoroutine(GetValidMoveTilesRoutine(type));
            routineOwner.StartCoroutine(GetValidTiles(type));
        }

        //TODO add type checking
        static IEnumerator GetValidTiles(HexLand.SelectType type = HexLand.SelectType.Same)
        {
            if (isRunning) yield break;
            isRunning = true;
            tiles.Clear();
            Object.Destroy(window);
            CharacterOverworld currentCOW = GameLogic.Instance.GetCurrentCOW();
            int points = currentCOW.m_CharacterStats.m_ActionPoints;
            rollCount = points;
            Plugin.Logger.LogWarning($"Begin loop: {Time.time}");
            Task task = Task.Factory.StartNew(() => tiles = [.. LoopNeighbors(currentCOW, points)]);
            yield return task.IsCompleted;
            Plugin.Logger.LogWarning($"end loop: {Time.time}");
            current.StartCoroutine(Wait());

            static IEnumerator Wait()
            {
                yield return new WaitForSeconds(0.5f);
                if (tiles.Count == 0) Plugin.Logger.LogError("no tiles found");
                window = MovementAction.RegisterAction(current.gameObject, tiles);
                isRunning = false;
            }
        }

        // alternate to GetValideMoveTiles
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
            Plugin.Logger.LogMessage($"loop count: {validNeighbors.Count}");
            return validNeighbors;
        }

        // unused due to GetRange issues
        static IEnumerator GetValidMoveTilesRoutine(HexLand.SelectType type)
        {
            if (isRunning)
            {
                yield break;
            }
            isRunning = true;
            tiles.Clear();
            Object.Destroy(window);
            CharacterOverworld current = GameLogic.Instance.GetCurrentCOW();
            int points = current.m_CharacterStats.m_ActionPoints;
            rollCount = points;
            // if (points > 6) points = 6;// to many tiles to check may cause issues
            Plugin.Logger.LogWarning($"current {points}");
            HexLand hex = current.GetHexLand();
            List<HexLand> tempList = [];
            Task task = Task.Factory.StartNew(()=> hex.GetRange(points, type, tempList)); // GetRange can give tiles out of range
            Plugin.Logger.LogMessage("start:" + (double)Time.time);
            // yield return new WaitUntil(new System.Func<bool>(() => task.IsCompleted));
            yield return task.IsCompleted;
            Plugin.Logger.LogMessage("end: " + (double)Time.time);
            if (task.IsCanceled || task.IsFaulted)
            {
                Plugin.Logger.LogError("move task failed");
                yield break;
            }
            if (tempList.Contains(hex)) tempList.Remove(hex);
            Plugin.Logger.LogMessage($"{tempList.Count} hexes found"); // incorrect numbers 21 found - 19 counted | works with smaller rolls (1=3)
            tiles = [.. tempList];
            current.StartCoroutine(Wait());

            IEnumerator Wait()
            {
                yield return new WaitForSeconds(0.5f);
                window = MovementAction.RegisterAction(current.gameObject, tiles);
                isRunning = false;
            }
        }


        [HarmonyPatch(typeof(uiMovementSlots), nameof(uiMovementSlots.Disengage))]// not called with movement or combat
        [HarmonyPostfix]
        static void MoveSlotsFadeOut()
        {
            Plugin.Logger.LogWarning("move slots fading out, dont know when called");
        }


        // [HarmonyPatch(typeof(Movement), nameof(Movement.StopTracking))] // not needed?
        // [HarmonyPostfix]
        // static void StopTracking()
        // {
        //     Plugin.Logger.LogMessage("stop tracking"); // end action?
        //     tiles.Clear();
        // }

    }
}

