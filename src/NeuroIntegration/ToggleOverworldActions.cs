using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    [HarmonyPatch]
    public class ToggleOverworldActions
    {
        static readonly List<INeuroAction> registeredActions = [];
        public static uiGameTrackerHUD.GameTrackerMode mode;

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.DungeonEncounter))]
        [HarmonyPrefix]
        static void Test1(CharacterOverworld __instance)
        {
            Plugin.Logger.LogMessage("dungeon encounter: " + __instance.m_CharacterStats.m_CharacterName);
        }

        [HarmonyPatch(typeof(DungeonScroller), nameof(DungeonScroller.DungeonExit))]
        [HarmonyPostfix]
        static void DungeonExit()
        {
            Plugin.Logger.LogMessage("dungeon exit");
            Context.Send("returning to overworld");
        }

        // to next room, from popup menu
        [HarmonyPatch(typeof(uiExploreDungeonMenu), nameof(uiExploreDungeonMenu.OnExplore))]
        [HarmonyPrefix]
        static void Test3()
        {
            Plugin.Logger.LogMessage("explore dungeon menu on explore");
        }

        [HarmonyPatch(typeof(uiEnterDungeonMenu), nameof(uiEnterDungeonMenu.OnEnter))]
        [HarmonyPrefix]
        static void Test4()
        {
            Plugin.Logger.LogMessage("enter dungeon menu OnEnter");
        }

        [HarmonyPatch(typeof(uiEnterDungeonMenu), nameof(uiEnterDungeonMenu.OnLeave))]
        [HarmonyPostfix]
        static void Test5()
        {
            Plugin.Logger.LogMessage("enter dungeon menu OnLeave");
        }

        // changes during battle loot screen
        [HarmonyPatch(typeof(uiGameTrackerHUD), nameof(uiGameTrackerHUD.ToggleGameTrackerMode))]
        [HarmonyPostfix]
        static void Test6(uiGameTrackerHUD.GameTrackerMode _mode)
        {
            mode = _mode;
            // string name = Enum.GetName(typeof(uiGameTrackerHUD.GameTrackerMode), _mode);
            // var test = Enum.Parse(typeof(uiGameTrackerHUD.GameTrackerMode), name);
            Plugin.Logger.LogMessage($"game track mode changed to {_mode}"); // game track mode changed to Overworld - Overworld - Overworld
            // if (_mode == uiGameTrackerHUD.GameTrackerMode.Overworld)
            // {
            //     QuickTimerCallback timer = new(EnableOverworldActions, 1000f); // maybe not. is set at end of battle during loot
            // }
            // else DisableOverworldActions();
        }
        
        public static void EnableOverworldActions(bool _override = false)
        {
            if (registeredActions.Count > 0 && !_override) return;
            DisableOverworldActions();
            NeuroAction queryLocation = new QueryCurrentCOWLocation();
            registeredActions.Add(queryLocation);
            NeuroActionHandler.RegisterActions(registeredActions);
        }

        public static void DisableOverworldActions()
        {
            NeuroActionHandler.UnregisterActions(registeredActions);
            registeredActions.Clear();
        }

        public static void AppendOverworldAction(NeuroAction action, bool _override = false)
        {
            NeuroAction existing = null;
            foreach (NeuroAction a in registeredActions.Cast<NeuroAction>())
            {
                if (a.Name == action.Name)
                {
                    existing = a;
                    break;
                }
            }
            if (existing == null)
            {
                registeredActions.Add(action);
                NeuroActionHandler.RegisterActions(registeredActions);
                return;
            }
            if (_override)
            {
                NeuroActionHandler.UnregisterActions(existing.Name);
                registeredActions.Remove(existing);
                registeredActions.Add(action);
                NeuroActionHandler.RegisterActions(registeredActions);
            }
        }
    }
}

// dungeon entered order
// dungeon encounter: sin
// [Info   : Unity Log] Combat: InitiateEncounterSessionRPC
// [Info   : Unity Log] Combat: InitiateCurrentEncounter
// [Info   : Unity Log] Combat: StartEncounterSession Dungeon baseCV Enemy
// [Info   : Unity Log] Encounter: Random Seed - 945920638
// [Message:Neuro For the King] game track mode changed to Dungeon - Dungeon - Dungeon
// [Message:Neuro For the King] game track mode changed to Dungeon - Dungeon - Dungeon
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 0
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 0
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 0
// [Info   : Unity Log] Sending ws message {"command":"actions/unregister","game":"For the King","data":{"action_names":["query_current_location","query_current_location","query_current_location"]}}
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 1
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 1
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 1
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 2
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 2
// [Info   : Unity Log] Combat: PlayIntroAnim_CR 2
// [Info   : Unity Log] CameraCut- UpdateEnd [ScrollEnd]
// [Info   : Unity Log] Combat: CommenceBattle
// [Info   : Unity Log] Combat: CommenceBattleRPC
// [Message:Neuro For the King] game track mode changed to Combat - Combat - Combat


// battle round finished
// [Info   : Unity Log] Combat: InitiateEncounterSessionRPC
// [Info   : Unity Log] Combat: InitiateCurrentEncounter


// next area
// [Info   : Unity Log] Game Round: 1, Stage Percent: 7%, Stage Progression: Tier1, Player Progression: Tier1
// [Message:Neuro For the King] explore dungeon menu on explore
// [Message:Neuro For the King] dungeon encounter: in pr
// [Info   : Unity Log] Combat: InitiateEncounterSessionRPC
// [Info   : Unity Log] Combat: InitiateCurrentEncounter
// [Info   : Unity Log] Combat: StartEncounterSession Dungeon  DungeonMiniEncounter
// [Info   : Unity Log] Encounter: Random Seed - 43250088
// [Message:Neuro For the King] game track mode changed to Dungeon - Dungeon - Dungeon
// [Info   : Unity Log] Sending ws message {"command":"actions/unregister","game":"For the King","data":{"action_names":[]}}
// [Info   : Unity Log] CameraCut- UpdateEnd [ScrollStart]
// [Message:Neuro For the King] game track mode changed to Dungeon - Dungeon - Dungeon
// [Info   : Unity Log] Sending ws message {"command":"actions/unregister","game":"For the King","data":{"action_names":[]}}
// [Info   : Unity Log] CameraCut- UpdateEnd [ScrollEndTrap]


// dungeon finished
// game track mode changed to Overworld - Overworld - Overworld
// dungeon exit
