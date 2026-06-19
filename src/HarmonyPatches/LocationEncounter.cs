using System.Collections;
using System.Collections.Generic;
using Google2u;
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

        [HarmonyPatch(typeof(uiLocationMenu), nameof(uiLocationMenu.GenerateMenuEntries))]
        [HarmonyPostfix]
        static void GenerateLocationMenuEntry(uiLocationMenu __instance)
        {
            List<uiLocationMenu.Entry> entries = __instance.m_MenuEntries;
            foreach (uiLocationMenu.Entry entry in entries)
            {
                Plugin.Logger.LogMessage($"text0 = {FTKHub.Localized<TextMenu>(entry.m_Text0)} || text1 = {entry.m_Text1}");
            }
        }

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
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), "SlideIn")]
        [HarmonyPostfix]
        static IEnumerator LocationMenuDisplayShow(IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
            window = LocationEncounterAction.RegisterAction(uiLocationMenuDisplay.Instance.gameObject, GetLocEncounterButtons(), GetLocEncounterFlavorText(), GetLocEncounterLoreDescription());
        }

        public static string GetLocEncounterFlavorText()
        {
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            string text = menu2.transform.Find("FlavorPopup").GetComponent<Text>().text;
            return StringReplace.RemoveStyling(text);
        }

        public static string GetLocEncounterLoreDescription()
        {
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            string text = menu2.transform.Find("LoreDescription").GetComponent<Text>().text;
            return text;
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
    }
}