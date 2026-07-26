using System.Collections.Generic;
using System.Linq;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

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
            menuDisplayValues = _miniHexInfo.GetMenuDisplayValues();
        }

        [HarmonyPatch(typeof(uiLocationMenuDisplay), nameof(uiLocationMenuDisplay.Show2))]
        [HarmonyPostfix]
        static void MenuDisplayPostShow()
        {
            Plugin.Logger.LogMessage("show location menu");
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Dungeon)
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
            ResetContextData();
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
            Object.Destroy(window);
        }

        static void CreateAction()
        {
            Object.Destroy(window);
            string ctx = GetEncounterContext(menuDisplayValues.m_Title, menuDisplayValues.m_Bottom, menuDisplayValues.m_Top);
            Context.Send(ctx);
            QuickTimerCallback timer = new(() =>
            {
                Dictionary<string, uiLocationMenuEntry> _buttons = GetLocEncounterButtons();
                // {string.Join(", ", [.. _buttons.Select(x => x.Key)])}
                string ctx = $"[buttons]";
                foreach (KeyValuePair<string, uiLocationMenuEntry> button in _buttons)
                {
                    string desc = button.Value.m_Text0.text;
                    string btnInfo = "";
                    if (GameDescriptions.EncounterDescriptions.TryGetValue(desc, out string _value))
                    {
                        btnInfo = _value;
                    }
                    ctx += $"{desc}: {btnInfo}\n";
                }
                window = LocationEncounterAction.RegisterAction(uiLocationMenuDisplay.Instance.gameObject, _buttons, ctx);
            }, uiLocationMenuDisplay.Instance.m_MenuPanel.gameObject);
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
            Plugin.Logger.LogMessage(string.Join(", ", [.. buttons.Select(x => x.Key)]));
            return buttons;
        }

        public static List<string> players = [];
        public static Dictionary<string, Dictionary<string, string>> enemies = [];
        static int count = 0;

        public static void ResetContextData()
        {
            players = [];
            enemies = [];
            count = 0;
        }

        public static string GetEncounterContext(string name, string description, string flavor)
        {
            string encounter = $"[Encounter] ({name}) {StringReplace.RemoveStyling(flavor)}; {StringReplace.RemoveStyling(description)}";
            string _players = "";
            if (players.Count > 0)
            {
                _players = $"[characters involved] {string.Join(", ", [.. players.Select(x => x)])}";
            }
            string _enemies = "";
            if (enemies.Count > 0)
            {
                _enemies = $"[enemies involved] {string.Join(", ", [.. enemies.Select(key => key.Value.Keys.First() + "(lvl " + key.Value.Values.First() + ")")])}";
            }
            return $"{encounter}\n{_players}\n{_enemies}";
        }

        [HarmonyPatch(typeof(uiEnemyEncounterPortrait), nameof(uiEnemyEncounterPortrait.Initialize))]
        [HarmonyPatch([typeof(string)])]
        [HarmonyPrefix]
        static void PortraitInitEnemy(string _enemyId)
        {
            if (_enemyId.IsNullOrEmpty() || _enemyId == "None")
            {
                enemies[count.ToString()] = new() {{"unknown", ""}};
                count++;
                return;
            }
            FTK_enemyCombat.ID id = FTK_enemyCombat.GetEnum(_enemyId);
            FTK_enemyCombat entry = FTK_enemyCombatDB.Get(id);
            string lvl = "";
            if (id != FTK_enemyCombat.ID.None && HauntManager.IsScourgeActive(HauntManager.Scourge.Deimos) && entry.CanBeRandomized())
            {
                id = FTK_enemyCombat.ID.None;
            }
            if (id != FTK_enemyCombat.ID.None) lvl = entry.GetEnemyLevelDisplay().ToString();
            enemies[count.ToString()] = new() { {entry.GetEnemyDisplay(), lvl}, };
            count++;
        }

        [HarmonyPatch(typeof(uiEnemyEncounterPortrait), nameof(uiEnemyEncounterPortrait.Initialize))]
        [HarmonyPatch([typeof(FTKPlayerID)])]
        [HarmonyPrefix]
        static void PortraitInitPlayer(FTKPlayerID _pid)
        {
            CharacterOverworld player = FTKHub.Instance.GetCharacterOverworldByFID(_pid);
            players.Add(player.m_CharacterStats.m_CharacterName);
        }


        [HarmonyPatch(typeof(uiLocationMenu), nameof(uiLocationMenu.GenerateMenuEntries))]
        [HarmonyPostfix]
        static void Location1(uiLocationMenu __instance)
        {
            List<uiLocationMenu.Entry> entries = __instance.m_MenuEntries;
            Plugin.Logger.LogWarning("loc_menu_generate");
            // Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => FTKHub.Localized<TextMenu>(x.m_Text0))])}"); // btn names
            // Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => x.m_Text1)])}"); // empty, probably mouseover descriptions
            // Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => x.m_Function)])}"); // call func when clicked
            // Plugin.Logger.LogWarning($"{string.Join(", ", [.. entries.Select(x => x.m_CheckFunction)])}");
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