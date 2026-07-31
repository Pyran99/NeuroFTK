using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class Encounters
    {

        #region encounter

        static ActionWindow window;
        static uiEncounterMenu encounterMenuInstance;
        static readonly Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> activeButtons = [];
        static string buttonsContext = "";
        static bool generating = false;


        [HarmonyPatch(typeof(SubPanelBaseBase), nameof(SubPanelBaseBase.GenerateMenu))]
        [HarmonyPrefix]
        static void ResetData()
        {
            activeButtons.Clear();
            buttonsContext = "";
        }

        [HarmonyPatch(typeof(SubPanelBaseBase), nameof(SubPanelBaseBase.GenerateMenu))]
        [HarmonyPostfix]
        static void SubMenuGenerated(SubPanelBaseBase __instance)
        {
            Plugin.Logger.LogWarning("SubPanelBaseBase.GenerateMenu");
            if (generating) return;
            generating = true;
            encounterMenuInstance = __instance.m_Owner;
            __instance.StartCoroutine(Wait(__instance.m_Buttons));

            static IEnumerator Wait(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> _buttons)
            {
                // wait for lower class to finish setup
                yield return new WaitForEndOfFrame();
                MiniHexInfo.MenuPOIDisplayValues values = encounterMenuInstance.m_ThisMiniHex?.GetMenuDisplayValues();
                string ctx = GetEncounterContext(values.m_Title, values.m_Bottom, values.m_Top);
                Context.Send(ctx);
                SetButtonData(_buttons);
                QuickTimerCallback timer = new (CreateEncounterAction, encounterMenuInstance.m_MainPanel.gameObject);
                // Context.Send(EncounterContext(instance.m_PoiName.text, instance.m_LoreDescription.text, instance.m_ThisMiniHex?.GetMenuDisplayValues().m_Top));
            }
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void DisableMenu()
        {
            Plugin.Logger.LogWarning("uiEncounterMenu.DisableMenu");
            Object.Destroy(window);
            ResetData();
            ResetContextData();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))]
        [HarmonyPostfix]
        static void MenuRefreshed()
        {
            Plugin.Logger.LogWarning("uiEncounterMenu.MenuRefresh");
            SubMenuGenerated(encounterMenuInstance.m_ActiveSubPanel);
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.LeaveOrEndTurn))]
        [HarmonyPostfix]
        static void Leave()
        {
            Plugin.Logger.LogWarning("LeaveOrEndTurn");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            Plugin.Logger.LogWarning("EndTurn");
        }

        [HarmonyPatch(typeof(uiEnemyPoiMenu), nameof(uiEnemyPoiMenu.SneakCallBack))]
        [HarmonyPrefix]
        static void SneakMovement2(uiSlotLegend.SlotOutput _output)
        {
            if (_output.m_Passed)
            {
                OverworldFlow.isSneakMovement = true;
                return;
            }
            OverworldFlow.isSneakMovement = false;
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
            players.Add(player);
        }

        static void CreateEncounterAction()
        {
            generating = false;
            if (!encounterMenuInstance.isActiveAndEnabled) return;
            Object.Destroy(window);
            window = EncounterAction.CreateWindow(encounterMenuInstance, [.. activeButtons.Values], buttonsContext);
        }

        // entered combat hex
        [HarmonyPatch(typeof(uiEncounterMenu), "SetMenuPanelMode")] // after getting buttons
        [HarmonyPostfix]
        static void Test3(uiEncounterMenu __instance)
        {
            MiniHexInfo.MiniHexType type = __instance.m_ThisMiniHex.m_MiniHexType;
            Plugin.Logger.LogMessage("uiEncounterMenu.SetMenuPanelMode");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.OpenOverworldTreasureChest))]
        [HarmonyPostfix]
        static void Test7()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.OpenOverworldTreasureChest");
        }
        
        // static readonly List<SubPanelBaseBase> menus = [];

        // public static SubPanelBaseBase GetActiveMenu()
        // {
        //     if (menus.Count == 0)
        //     {
        //         Plugin.Logger.LogError("no menu in list");
        //         return null;
        //     }
        //     return menus.First(x => x.isActiveAndEnabled);
        // }

        // static void SetupMenuList(uiEncounterMenu _instance)
        // {
        //     if (menus.Count == 0)
        //     {
        //         menus.Add(_instance.m_EnemyPanel);
        //         menus.Add(_instance.m_DeadAdventurerPanel);
        //         menus.Add(_instance.m_RevivalPanel);
        //         menus.Add(_instance.m_WishingWellPanel);
        //         menus.Add(_instance.m_SkillTestPanel);
        //         menus.Add(_instance.m_ServicePanel);
        //         menus.Add(_instance.m_CarnivalMenu);
        //         menus.Add(_instance.m_GambleDenMenu);
        //     }
        // }

        #endregion



        #region location menu

        public static List<CharacterOverworld> players = [];
        public static Dictionary<string, Dictionary<string, string>> enemies = [];
        static int count = 0;

        public static void ResetContextData()
        {
            players = [];
            enemies = [];
            count = 0;
        }

        /// <summary>
        /// info about encounter, characters involved
        /// </summary>
        /// <returns>"{encounter}\n{_players}\n{_enemies}"</returns>
        public static string GetEncounterContext(string name, string description, string flavor)
        {
            string encounter = $"[Encounter] ({name}) {StringReplace.RemoveStyling(flavor)}; {StringReplace.RemoveStyling(description)}\n";
            StringBuilder sbPlayers = new();
            foreach (CharacterOverworld player in players)
            {
                sbPlayers.AppendLine($"{CharacterData.GetCharacterName(player)} (lvl {player.m_CharacterStats.m_PlayerLevel})");
            }
            // if (players.Count > 0)
            // {
            //     _players = $"[characters involved] {string.Join(", ", [.. players.Select(CharacterData.GetCharacterName)])}";
            // }
            string _enemies = "";
            if (enemies.Count > 0)
            {
                _enemies = $"[enemies involved] {string.Join(", ", [.. enemies.Select(key => key.Value.Keys.First() + "(lvl " + key.Value.Values.First() + ")")])}";
            }
            return $"{encounter}{sbPlayers}{_enemies}";
        }

        /// <summary>
        /// sets buttonsContext
        /// </summary>
        static void SetButtonData(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> buttons)
        {
            activeButtons.Clear();
            foreach (KeyValuePair<SubPanelBaseBase.ButtonID, uiPoiButton> kvp in buttons)
            {
                if (!kvp.Value.isActiveAndEnabled || kvp.Value.m_ButtonLock) continue;
                if (activeButtons.ContainsKey(kvp.Key)) continue;
                activeButtons.Add(kvp.Key, kvp.Value);
            }
            Dictionary<string, string> flavorData = [];
            Dictionary<string, object> rollData = [];
            foreach (uiPoiButton btn in activeButtons.Values)
            {
                GetButtonData(btn, flavorData, rollData);
            }
            StringBuilder sb = new("(this encounters actions (actions with no roll results will always succeed) displayed as: [action (description)] total successful rolls(chance for this result) = outcome result)\n");
            foreach (KeyValuePair<string, object> data in rollData)
            {
                // [ambush (ambush flavor)]
                sb.AppendLine($"[{data.Key} ({flavorData[data.Key]})]");
                foreach (KeyValuePair<string, Dictionary<string, string>> outcome in (Dictionary<string, Dictionary<string, string>>)data.Value)
                {
                    // string value = JsonConvert.SerializeObject(outcome.Value);
                    // 0(2%) = Failure
                    sb.AppendLine($"{outcome.Key}({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}");
                }
            }
            MiniHexInfo.PoiProfile profile = encounterMenuInstance.m_ThisMiniHex.GetPOIProfile();
            if (profile != null && profile.m_SkillRequired != FTK_weaponStats2.SkillType.none)
            {
                sb.Append($"these roll chances are based on your {profile.m_SkillRequired} stat");
            }
            buttonsContext = sb.ToString();
        }

        /// <summary>
        /// adds data to flavorData and rollData
        /// </summary>
        static void GetButtonData(uiPoiButton btn, Dictionary<string, string> flavorData, Dictionary<string, object> rollData)
        {
                if (flavorData.ContainsKey(btn.m_ButtonText.text)) return;
                if (rollData.ContainsKey(btn.m_ButtonText.text)) return;
                flavorData.Add(btn.m_ButtonText.text, GameDescriptions.GetEncounterBtnFlavor(btn.m_ButtonInfo.m_ButtonType));
                FTK_slotOutput.ID id = FTK_slotOutput.ID.None;
                if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Ambush)
                {
                    id = RollSlotOutcomes._getAmbushType((MiniHexEnemy)encounterMenuInstance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                else if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Sneak)
                {
                    id = RollSlotOutcomes._getSneakType((MiniHexEnemy)encounterMenuInstance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                if (id == FTK_slotOutput.ID.None)
                {
                    MiniEncounter hex = encounterMenuInstance.m_ThisMiniHex as MiniEncounter;
                    if (hex?.GetDBEntry() != null)
                    {
                        id = hex.GetDBEntry().m_SlotRoll;
                    }
                }
                Dictionary<string, Dictionary<string, string>> outcome;
                // ExitFunc means no rolls
                if (string.IsNullOrEmpty(btn.m_ButtonInfo.m_ExitFunc))
                {
                    outcome = [];
                }
                else
                {
                    outcome = RollSlotOutcomes.GetOutcomes(GameLogic.Instance.GetCurrentCOW(), id);
                }
                // { "ambush": { 0: {5%: failure} }, { 1: {5%: success} }
                rollData.Add(btn.m_ButtonText.text, outcome);
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

        #endregion


    }
}