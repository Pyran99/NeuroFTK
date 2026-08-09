using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ToggleDisposableActions
    {
        static readonly List<INeuroAction> overworldActions = [];
        static readonly List<INeuroAction> combatActions = [];

        public static void ToggleOverworldActions(bool enable, bool overwrite = true)
        {
            if (enable)
            {
                if (overworldActions.Count > 0)
                {
                    if (overwrite)
                    {
                        NeuroActionHandler.UnregisterActions(overworldActions);
                    }
                    else if (overworldActions.Count < 5) // same as list below
                    {
                        NeuroActionHandler.UnregisterActions(overworldActions);
                    }
                    else return;
                }
                overworldActions.Clear();
                CharacterOverworld cow = CharacterData.GetNeuroCow();
                if (cow.IsInDungeon() || cow.m_CharacterStats.m_IsInCombat)
                {
                    Plugin.Logger.LogWarning("tried to register overworld actions in combat");
                    return;
                }
                INeuroAction queryLocation = new QueryCurrentCOWLocation();
                overworldActions.Add(queryLocation);
                INeuroAction queryBeltItems = new QueryBeltItems();
                overworldActions.Add(queryBeltItems);
                INeuroAction queryStatus = new QueryStatusEffects();
                overworldActions.Add(queryStatus);
                INeuroAction zoomCamera = new CameraZoomAction(FTKHub.Instance.m_OverworldCamera.GetComponent<RtsCamera>());
                overworldActions.Add(zoomCamera);
                INeuroAction spinCamera = new CameraSpinAction();
                overworldActions.Add(spinCamera);
                Plugin.Instance.StartCoroutine(RegisterWait(overworldActions));
            }
            else
            {
                if (overworldActions.Count == 0) return;
                NeuroActionHandler.UnregisterActions(overworldActions);
                overworldActions.Clear();
            }
        }

        public static void ToggleCombatActions(bool enable, bool overwrite = true)
        {
            // belt, status, 
            if (enable)
            {
                if (combatActions.Count > 0)
                {
                    if (overwrite)
                    {
                        NeuroActionHandler.UnregisterActions(combatActions);
                    }
                    else return;
                }
                combatActions.Clear();
                CharacterOverworld cow = CharacterData.GetNeuroCow();
                if ((!cow.IsInDungeon() && !cow.m_CharacterStats.m_IsInCombat) || GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Overworld)
                {
                    Plugin.Logger.LogWarning("tried to register combat actions in overworld");
                    return;
                }
                INeuroAction queryBeltItems = new QueryBeltItems();
                combatActions.Add(queryBeltItems);
                INeuroAction queryStatus = new QueryStatusEffects();
                combatActions.Add(queryStatus);
                INeuroAction sendMsg = new SillyAction();
                combatActions.Add(sendMsg);
                INeuroAction spinCamera = new CameraSpinAction();
                combatActions.Add(spinCamera);
                INeuroAction jumpAction = new CowJumpAction();
                combatActions.Add(jumpAction);
                Plugin.Instance.StartCoroutine(RegisterWait(combatActions));
            }
            else
            {
                if (combatActions.Count == 0) return;
                NeuroActionHandler.UnregisterActions(combatActions);
                combatActions.Clear();
            }
        }

        static IEnumerator RegisterWait(List<INeuroAction> actions)
        {
            yield return null;
            NeuroActionHandler.RegisterActions(actions);
        }

        public static void AppendOverworldAction(INeuroAction action, bool overwrite = false)
        {
            INeuroAction existing = null;
            foreach (INeuroAction a in overworldActions)
            {
                if (a.Name == action.Name)
                {
                    existing = a;
                    break;
                }
            }
            if (existing == null)
            {
                overworldActions.Add(action);
                NeuroActionHandler.RegisterActions(action);
                return;
            }
            if (overwrite)
            {
                NeuroActionHandler.UnregisterActions(existing.Name);
                overworldActions.Remove(existing);
                overworldActions.Add(action);
                NeuroActionHandler.RegisterActions(overworldActions);
            }
        }

        public static void AppendCombatAction(INeuroAction action, bool overwrite = false)
        {
            INeuroAction existing = null;
            foreach (INeuroAction a in combatActions)
            {
                if (a.Name == action.Name)
                {
                    existing = a;
                    break;
                }
            }
            if (existing == null)
            {
                combatActions.Add(action);
                NeuroActionHandler.RegisterActions(action);
                return;
            }
            if (overwrite)
            {
                NeuroActionHandler.UnregisterActions(existing.Name);
                combatActions.Remove(existing);
                combatActions.Add(action);
                NeuroActionHandler.RegisterActions(combatActions);
            }
        }

        public static void DisposeAction(INeuroAction action)
        {
            overworldActions.Remove(action);
            combatActions.Remove(action);
            NeuroActionHandler.UnregisterActions(action);
        }
    }
}