using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        static MiniHexInfo miniHexInfo;
        static MiniHexInfo.MenuPOIDisplayValues menuDisplayValues;
        static ActionWindow window;

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPrefix]
        static void MenuDisplayPreShow(MiniHexInfo _miniHexInfo, uiLocationMenuDisplay __instance)
        {
            miniHexInfo = _miniHexInfo;
            menuDisplayValues = _miniHexInfo.GetMenuDisplayValues();
            locationMenuInstance = __instance;
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPostfix]
        static void MenuDisplayPostShow()
        {
            Plugin.Logger.LogWarning("uiLocationMenuDisplay.Show2");
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Dungeon)
            {
                Plugin.Logger.LogWarning("uiLocationMenuDisplay.Show2 skipped in dungeon mode");
                return;
            }
            CreateLocationAction();
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Unhide))]
        [HarmonyPostfix]
        static void MenuDisplayUnhide()
        {
            Plugin.Logger.LogWarning("uiLocationMenuDisplay.Unhide");
            CreateLocationAction();
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
            Plugin.Logger.LogMessage("uiLocationMenuDisplay.StartShutdown");
            Encounters.ResetContextData();
            Object.Destroy(window);
            menuDisplayValues = null;
            miniHexInfo = null;
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.SlideOutMainMenu))]
        [HarmonyPrefix]
        static void MenuDisplaySlideOut() // remove main actions
        {
            Plugin.Logger.LogWarning("uiLocationMenuDisplay.SlideOutMainMenu");
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.SwitchToSubMenu))]
        [HarmonyPostfix]
        static void SwitchToSubMenu() // create new menu actions (unless handled elsewhere? shop uiBuyMenu)
        {
            Plugin.Logger.LogWarning("uiLocationMenuDisplay.SwitchToSubMenu");
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(uiLocationMenu), nameof(uiLocationMenu.GenerateMenuEntries))]
        [HarmonyPostfix]
        static void GenerateEntries(uiLocationMenu __instance)
        {
            Plugin.Logger.LogWarning("loc_menu_generate");
            List<uiLocationMenu.Entry> entries = __instance.m_MenuEntries;
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
            Context.Send($"you threw some gold into the well, your chance of a success drink increased");
        }

        [HarmonyPatch(typeof(uiWishingWellMenu), nameof(uiWishingWellMenu.UseDrinkWellButton))]
        [HarmonyPrefix]
        static void OnDrinkWell()
        {
            Context.Send($"you drank from the well");
        }

        [HarmonyPatch(typeof(MiniHexDungeon), nameof(MiniHexDungeon.GenerateDungeonEncounters))]
        [HarmonyPrefix]
        static void GeneratingDungeon() // VERIFY attempt fix move actions register on dungeon entering
        {
            Plugin.Logger.LogWarning("GeneratingDungeon");
            GameStates.mode = uiGameTrackerHUD.GameTrackerMode.Dungeon;
        }

        static void CreateLocationAction()
        {
            Plugin.Logger.LogWarning("create location encounter window");
            string ctx = Encounters.GetEncounterContext(menuDisplayValues.m_Title, menuDisplayValues.m_Bottom, menuDisplayValues.m_Top);
            if (locationMenuInstance.m_DifficultyRoot.gameObject.activeInHierarchy)
            {
                ctx += $"\nthis encounters enemies are lvl {locationMenuInstance.m_Difficulty.text}";
            }
            Context.Send(ctx);
            QuickTimerCallback timer = new(CreateActionWindow, uiLocationMenuDisplay.Instance.m_MenuPanel.gameObject);
        }

        public static void CreateActionWindow()
        {
            Dictionary<string, uiLocationMenuEntry> _buttons = GetLocEncounterButtons();
            StringBuilder sb = new("[buttons]");
            foreach (KeyValuePair<string, uiLocationMenuEntry> button in _buttons)
            {
                string desc = button.Value.m_Text0.text;
                string btnInfo = "";
                if (GameDescriptions.EncounterDescriptions.TryGetValue(desc, out string _value))
                {
                    btnInfo = _value;
                }
                sb.AppendLine($"{desc}: {btnInfo}");
            }
            int cost = miniHexInfo.GetCost(CharacterData.GetNeuroCow());
            if (cost > 0) sb.AppendLine($"this encounter costs {cost} gold, the current character has {CharacterData.GetNeuroCow().m_CharacterStats.m_Gold} gold");
            window = LocationEncounterAction.RegisterAction(uiLocationMenuDisplay.Instance.gameObject, _buttons, sb.ToString());
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
            Plugin.Logger.LogMessage("btns: " + string.Join(", ", [.. buttons.Select(x => x.Key)]));
            return buttons;
        }
    }
}