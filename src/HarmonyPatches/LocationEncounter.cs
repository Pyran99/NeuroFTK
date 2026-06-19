using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
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

        [HarmonyPatch(typeof(uiLocationMenuDisplay), "Shutdown2")]
        [HarmonyPrefix]
        static void LocationMenuClosed()
        {
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.StartShutdown))] // before tracking resumes
        [HarmonyPrefix]
        static void StartShutdown()
        {
            Object.Destroy(window);
            menuDisplayValues = null;
            miniHexInfo = null;
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPrefix]
        static void Show(MiniHexInfo _miniHexInfo)
        {
            miniHexInfo = _miniHexInfo;
            menuDisplayValues = _miniHexInfo.GetMenuDisplayValues(); // translates
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), "SlideIn")]
        [HarmonyPostfix]
        static IEnumerator LocationMenuDisplayShow(IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
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

        // [HarmonyPatch(typeof(uiLocationMenu), nameof(uiLocationMenu.GenerateMenuEntries))]
        // [HarmonyPostfix]
        // static void GenerateLocationMenuEntry(uiLocationMenu __instance)
        // {
        //     List<uiLocationMenu.Entry> entries = __instance.m_MenuEntries;
        //     foreach (uiLocationMenu.Entry entry in entries)
        //     {
        //         Plugin.Logger.LogMessage($"text0 = {FTKHub.Localized<TextMenu>(entry.m_Text0)} || text1 = {entry.m_Text1}");
        //     }
        // }
    }
}