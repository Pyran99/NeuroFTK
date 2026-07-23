using System.Collections.Generic;
using NeuroSdk.Actions;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ToggleDisposableActions
    {
        static readonly List<INeuroAction> overworldActions = [];
        static readonly List<INeuroAction> combatActions = [];

        public static void ToggleOverworldActions(bool enable, bool overwrite = true)
        {
            // belt, location, 
            if (enable)
            {
                if (overworldActions.Count > 0 && !overwrite) return;
                NeuroActionHandler.UnregisterActions(overworldActions);
                overworldActions.Clear();
                CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
                if (cow.IsInDungeon())
                {
                    Plugin.Logger.LogWarning("tried to register overworld actions in dungeon");
                    return;
                }
                INeuroAction queryLocation = new QueryCurrentCOWLocation();
                overworldActions.Add(queryLocation);
                INeuroAction queryBeltItems = new QueryBeltItems(cow);
                overworldActions.Add(queryBeltItems);
                NeuroActionHandler.RegisterActions(overworldActions);
            }
            else
            {
                NeuroActionHandler.UnregisterActions(overworldActions);
                overworldActions.Clear();
            }
        }

        public static void ToggleCombatActions(bool enable, bool overwrite)
        {
            // belt, 
            if (enable)
            {
                if (combatActions.Count > 0 && !overwrite) return;
                NeuroActionHandler.UnregisterActions(combatActions);
                combatActions.Clear();
                CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
                if (!cow.IsInDungeon())
                {
                    Plugin.Logger.LogWarning("tried to register combat actions in overworld");
                    return;
                }
                INeuroAction queryBeltItems = new QueryBeltItems(cow);
                combatActions.Add(queryBeltItems);
                NeuroActionHandler.RegisterActions(combatActions);
            }
            else
            {
                NeuroActionHandler.UnregisterActions(combatActions);
                combatActions.Clear();
            }
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
                NeuroActionHandler.RegisterActions(overworldActions);
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
                NeuroActionHandler.RegisterActions(combatActions);
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
            if (overworldActions.Remove(action))
            {
                NeuroActionHandler.UnregisterActions(action);
            }
            if (combatActions.Remove(action))
            {
                NeuroActionHandler.UnregisterActions(action);
            }
        }
    }
}