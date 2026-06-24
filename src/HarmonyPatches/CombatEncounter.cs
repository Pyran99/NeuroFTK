using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CombatEncounter
    {

#region Main

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EnableMenu))]
        [HarmonyPostfix]
        static void EncounterMenuEnabled()
        {
            Plugin.Logger.LogMessage("NYI uiEncounterMenu.EnableMenu => send neuro action of choices");
            ToggleOverworldActions.DisableOverworldActions();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void Test5()
        {
            Plugin.Logger.LogMessage("8 disable menu");
            // ToggleOverworldActions.EnableOverworldActions(); //TODO enable from elsewhere, this is called for leave & enter encounter
        }


#endregion


        #region may have uses

        // entered combat hex
        [HarmonyPatch(typeof(uiEncounterMenu), "SetMenuPanelMode")] // may have uses
        [HarmonyPostfix]
        static void Test3(uiEncounterMenu __instance)
        {
            MiniHexInfo.MiniHexType type = __instance.m_ThisMiniHex.m_MiniHexType;
            Plugin.Logger.LogMessage("uiEncounterMenu.SetMenuPanelMode");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))] // may have uses
        [HarmonyPostfix]
        static void Test2()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.MenuRefresh");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.OpenOverworldTreasureChest))]
        [HarmonyPostfix]
        static void Test7()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.OpenOverworldTreasureChest");
        }
        
        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.LeaveOrEndTurn))]
        [HarmonyPostfix]
        static void Test6()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.LeaveOrEndTurn");
        }


        #endregion
    }
}