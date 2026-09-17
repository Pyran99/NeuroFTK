using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class EncounterLocation
    {
        static uiLocationMenuDisplay locationMenuInstance;
        static CharacterOverworld cow;
        static MiniHexInfo miniHexInfo;
        static MiniHexInfo.MenuPOIDisplayValues menuDisplayValues;
        static ActionWindow window;

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPrefix]
        static void MenuDisplayPreShow(MiniHexInfo _miniHexInfo, CharacterOverworld _cow, uiLocationMenuDisplay __instance)
        {
            miniHexInfo = _miniHexInfo;
            menuDisplayValues = _miniHexInfo.GetMenuDisplayValues();
            locationMenuInstance = __instance;
            cow = _cow;
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPostfix]
        static void MenuDisplayPostShow()
        {
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Dungeon)
            {
                Plugin.Logger.LogWarning("uiLocationMenuDisplay.Show2 skipped in dungeon mode");
                return;
            }
            CreateLocationAction(cow);
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Unhide))]
        [HarmonyPostfix]
        static void MenuDisplayUnhide()
        {
            CreateLocationAction(cow);
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), "Shutdown2")]
        [HarmonyPrefix]
        static void LocationMenuClosed()
        {
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.StartShutdown))] // before tracking resumes, called after dungeon battle
        [HarmonyPrefix]
        static void StartShutdown()
        {
            Encounters.ResetContextData();
            Object.Destroy(window);
            menuDisplayValues = null;
            miniHexInfo = null;
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.SlideOutMainMenu))]
        [HarmonyPrefix]
        static void MenuDisplaySlideOut() // remove main actions
        {
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.SwitchToSubMenu))]
        [HarmonyPostfix]
        static void SwitchToSubMenu() // create new menu actions (unless handled elsewhere? shop uiBuyMenu)
        {
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(uiLocationMenu), nameof(uiLocationMenu.GenerateMenuEntries))]
        [HarmonyPostfix]
        static void GenerateEntries(uiLocationMenu __instance)
        {
            // List<uiLocationMenu.Entry> entries = __instance.m_MenuEntries;
            // m_Text0 // btn name
            // m_Text1 // maybe mouseover description
            // m_Function // func to call when clicked
            // m_CheckFunction // ??
        }

        [HarmonyPatch(typeof(uiLocationMenuEntry), nameof(uiLocationMenuEntry.SetEntry))] // menu buttons
        [HarmonyPostfix]
        static void SetEntry(uiLocationMenuEntry __instance)
        {
            // Plugin.Logger.LogWarning($"{__instance.m_Menu?.m_Location?.GetType()}"); //MiniEncounter | MiniHexTown
        }


        [HarmonyPatch(typeof(uiWishingWellMenu), nameof(uiWishingWellMenu.UseThrowCoinsButton))]
        [HarmonyPostfix]
        static void OnWellCoinsThrown()
        {
            Context.Send($"you threw some gold into the well, your chance of a successful drink increased");
        }

        [HarmonyPatch(typeof(uiWishingWellMenu), nameof(uiWishingWellMenu.UseDrinkWellButton))]
        [HarmonyPrefix]
        static void OnDrinkWell()
        {
            Context.Send($"you drank from the well");
        }

        [HarmonyPatch(typeof(MiniHexDungeon), nameof(MiniHexDungeon.GenerateDungeonEncounters))]
        [HarmonyPrefix]
        static void GeneratingDungeon()
        {
            GameStates.mode = uiGameTrackerHUD.GameTrackerMode.Dungeon;
        }

        static void CreateLocationAction(CharacterOverworld _cow)
        {
            Plugin.Logger.LogMessage("create location encounter window");
            if (!Multiplayer.OtherPlayersAction(_cow)) return;
            bool isDungeon = miniHexInfo is MiniHexDungeon;
            StringBuilder sb = new(Encounters.GetEncounterContext(menuDisplayValues.m_Title, menuDisplayValues.m_Bottom, menuDisplayValues.m_Top, locationMenuInstance.m_Cost, locationMenuInstance.m_Difficulty, isDungeon));
            Context.Send(sb.ToString());
            uiLocationMenuDisplay.Instance.StartCoroutine(QuickTimerCallback.WaitRoutine(CreateActionWindow, uiLocationMenuDisplay.Instance.m_MenuPanel.gameObject));
        }

        public static void CreateActionWindow()
        {
            Dictionary<string, uiLocationMenuEntry> _buttons = GetLocEncounterButtons();
            StringBuilder sb = new("## buttons \n");
            foreach (KeyValuePair<string, uiLocationMenuEntry> button in _buttons)
            {
                string desc = button.Value.m_Text0.text;
                string btnInfo = "";
                if (GameDescriptions.EncounterDescriptions.TryGetValue(desc, out string _value))
                {
                    btnInfo = _value;
                }
                sb.AppendLine($"- {desc}: {btnInfo}");
            }
            int cost = miniHexInfo.GetCost(CharacterData.GetActiveCow());
            if (cost > 0) sb.AppendLine($"this encounter costs {cost} gold, the current character has {CharacterData.GetActiveCow().m_CharacterStats.m_Gold} gold");
            window = LocationEncounterAction.RegisterAction(uiLocationMenuDisplay.Instance.gameObject, _buttons, sb.ToString().TrimEnd(['\n']));
            UnregisterDisabledObject.QuickCreate(uiLocationMenuDisplay.Instance.transform.Find("mainMenu").gameObject, window);
        }

        public static Dictionary<string, uiLocationMenuEntry> GetLocEncounterButtons()
        {
            GameObject menu1 = uiLocationMenuDisplay.Instance.gameObject.transform.Find("mainMenu").gameObject;
            GameObject menu2 = menu1.transform.Find("mainMenu").gameObject;
            GameObject panel = menu2.transform.Find("MenuPanel").gameObject;
            Dictionary<string, uiLocationMenuEntry> buttons = [];
            foreach (Transform child in panel.transform)
            {
                uiLocationMenuEntry entry = child.GetComponent<uiLocationMenuEntry>();
                if (entry == null) continue;
                if (!entry.m_Button.interactable) continue;
                Text comp = child.GetComponentInChildren<Text>();
                buttons.Add(comp.text, entry);
            }
            return buttons;
        }
    }
}