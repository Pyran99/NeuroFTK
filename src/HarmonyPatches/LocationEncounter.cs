using System.Collections.Generic;
using System.Linq;
using Google2u;
using HarmonyLib;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class LocationEncounter
    {
        static ActionWindow window;
        static MiniHexInfo miniHexInfo;
        static MiniHexInfo.MenuPOIDisplayValues menuDisplayValues;


        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPrefix]
        static void MenuDisplayPreShow(MiniHexInfo _miniHexInfo)
        {
            miniHexInfo = _miniHexInfo;
            menuDisplayValues = _miniHexInfo.GetMenuDisplayValues(); // translates
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPostfix]
        static void MenuDisplayPostShow()
        {
            Plugin.Logger.LogMessage("show location menu");
            if (ToggleOverworldActions.mode == uiGameTrackerHUD.GameTrackerMode.Dungeon)
            {
                Plugin.Logger.LogWarning("dungeon loc_menu skipped");
                return;
            }
            CreateAction();
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Unhide))]
        [HarmonyPostfix]
        static void MenuDisplayUnhide()
        {
            Plugin.Logger.LogMessage("unhide menu");
            CreateAction();
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), "Shutdown2")]
        [HarmonyPrefix]
        static void LocationMenuClosed()
        {
            
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.StartShutdown))] // before tracking resumes, called after dungeon battle
        [HarmonyPrefix]
        static void StartShutdown()// remove all location actions
        {
            Plugin.Logger.LogMessage("close location menu");
            Object.Destroy(window);
            menuDisplayValues = null;
            miniHexInfo = null;
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.SlideOutMainMenu))]
        [HarmonyPrefix]
        static void MenuDisplaySlideOut() // remove main actions
        {
            Plugin.Logger.LogWarning("loc_display_SlideOutMainMenu");
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.SwitchToSubMenu))]
        [HarmonyPostfix]
        static void SwitchToSubMenu() // create new menu actions (unless handled elsewhere? shop uiBuyMenu)
        {
            Plugin.Logger.LogWarning("loc_display_SwitchSubMenu");
            Plugin.Logger.LogWarning("TODO submenu actions");
        }

        static void CreateAction()
        {
            Object.Destroy(window);
            string info = $"{GetLocEncounterName()}: {GetLocEncounterFlavorText()}";
            window = LocationEncounterAction.RegisterAction(uiLocationMenuDisplay.Instance.gameObject, GetLocEncounterButtons(), info, GetLocEncounterLoreDescription());
        }

        public static string GetLocEncounterName()
        {
            if (menuDisplayValues != null)
            {
                return menuDisplayValues.m_Title;
            }
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            GameObject header = menu2.transform.Find("LocationHeader").gameObject;
            Text comp = header.GetComponentInChildren<Text>();
            if (comp != null) return comp.text;
            return "";
        }

        public static string GetLocEncounterFlavorText()
        {
            if (menuDisplayValues != null)
            {
                return StringReplace.RemoveStyling(menuDisplayValues.m_Top);
            }
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            string text = menu2.transform.Find("FlavorPopup").GetComponent<Text>().text;
            return StringReplace.RemoveStyling(text);
        }

        public static string GetLocEncounterLoreDescription()
        {
            if (menuDisplayValues != null)
            {
                return StringReplace.RemoveStyling(menuDisplayValues.m_Bottom);
            }
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            string text = menu2.transform.Find("LoreDescription").GetComponent<Text>().text;
            return StringReplace.RemoveStyling(text);
        }

        public static Dictionary<string, uiLocationMenuEntry> GetLocEncounterButtons()
        {
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            GameObject panel = menu2.transform.Find("MenuPanel").gameObject;
            Dictionary<string, uiLocationMenuEntry> buttons = [];
            foreach (Transform child in panel.transform)
            {
                if (child.GetComponent<uiLocationMenuEntry>() == null) continue;
                Text comp = child.GetComponentInChildren<Text>();
                buttons.Add(comp.text, child.GetComponent<uiLocationMenuEntry>());
            }
            return buttons;
        }

        [HarmonyPatch(typeof(uiLocationMenu), nameof(uiLocationMenu.GenerateMenuEntries))]
        [HarmonyPostfix]
        static void Location1(uiLocationMenu __instance)
        {
            List<uiLocationMenu.Entry> entries = __instance.m_MenuEntries;
            Plugin.Logger.LogWarning("loc_menu_generate");
            Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => FTKHub.Localized<TextMenu>(x.m_Text0))])}"); // btn names
            Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => x.m_Text1)])}"); // empty, probably mouseover descriptions
            Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => x.m_Function)])}"); // call func when clicked
            Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => x.m_CheckFunction)])}");
        }

        [HarmonyPatch(typeof(uiLocationMenuEntry), nameof(uiLocationMenuEntry.SetEntry))] // menu buttons
        [HarmonyPostfix]
        static void Location2(uiLocationMenuEntry __instance)
        {
            // Plugin.Logger.LogWarning("loc_entry_set");
            // Plugin.Logger.LogWarning($"{__instance.m_Menu?.m_Location?.GetType()}"); //MiniEncounter | MiniHexTown
        }
    }
}