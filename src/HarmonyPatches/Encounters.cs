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
        public static uiEncounterMenu EncounterMenuInstance { get; private set; }
        public static List<CharacterOverworld> involvedPlayers = [];
        /// <summary>
        /// involvedEnemies[unitDupeCount.ToString()] = new() { {entry.GetEnemyDisplay(), lvl}, };
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> involvedEnemies = [];
        public static readonly Dictionary<string, uiPoiButton> activeButtons = [];
        public static Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> allButtons = [];
        
        static ActionWindow window;
        static string buttonsContext = "";
        static bool generating = false;
        static bool isJournal = false;
        static int unitDupeCount = 0;
        static List<FTK_slotOutput> _CarnivalOptions = [];


        [HarmonyPatch(typeof(SubPanelBaseBase), nameof(SubPanelBaseBase.GenerateMenu))]
        [HarmonyPrefix]
        static void ResetData()
        {
            activeButtons.Clear();
            buttonsContext = "";
            _CarnivalOptions.Clear();
        }

        [HarmonyPatch(typeof(SubPanelBaseBase), nameof(SubPanelBaseBase.GenerateMenu))]
        [HarmonyPostfix]
        static void SubMenuGenerated(SubPanelBaseBase __instance)
        {
            if (generating) return; // called twice
            if (Multiplayer.OtherPlayersAction(__instance.CurrentCow)) return;
            Plugin.Logger.LogMessage("panel encounter type = " + __instance.GetType());
            generating = true;
            EncounterMenuInstance = __instance.m_Owner;
            allButtons = new(__instance.m_Buttons);
            EncounterMenuInstance.m_ActiveSubPanel.StartCoroutine(DelayActions(allButtons));
        }

        public static IEnumerator DelayActions(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> _buttons)
        {
            // wait for lower class to finish setup
            Object.Destroy(window);
            yield return null;
            if (!SetButtonData(_buttons))
            {
                Plugin.Logger.LogMessage("reading journal");
                yield break;
            }
            EncounterMenuInstance.StartCoroutine(QuickTimerCallback.WaitRoutine(() => CreateEncounterAction(EncounterMenuInstance.m_ActiveSubPanel), EncounterMenuInstance.m_ActiveSubPanel.gameObject));
        }

        [HarmonyPatch(typeof(uiCarnivalMenu), "CreateCarnivalOptions")]
        [HarmonyPostfix]
        static void CarnivalOptions(ref List<FTK_slotOutput> ___m_CarnivalOptions)
        {
            Plugin.Logger.LogMessage("set carnival options");
            _CarnivalOptions = [.. ___m_CarnivalOptions];
        }

        [HarmonyPatch(typeof(uiGambleDenMenu), nameof(uiGambleDenMenu.UseBuyInButton))]
        [HarmonyPostfix]
        static void EnterGambleDen(SubPanelBaseBase __instance)
        {
            Plugin.Logger.LogMessage("enter gamble den");
            ResetData();
            SubMenuGenerated(__instance);
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void DisableMenu()
        {
            Object.Destroy(window);
            ResetData();
            ResetContextData();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))]
        [HarmonyPostfix]
        static void MenuRefreshed()
        {
            SubMenuGenerated(EncounterMenuInstance.m_ActiveSubPanel);
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.LeaveOrEndTurn))]
        [HarmonyPostfix]
        static void Leave()
        {
            Plugin.Logger.LogWarning("uiEncounterMenu.LeaveOrEndTurn");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            Plugin.Logger.LogWarning("uiEncounterMenu.EndTurn");
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
                involvedEnemies[unitDupeCount.ToString()] = new() {{"unknown", ""}};
                unitDupeCount++;
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
            involvedEnemies[unitDupeCount.ToString()] = new() { {entry.GetEnemyDisplay(), lvl}, };
            unitDupeCount++;
        }

        [HarmonyPatch(typeof(uiEnemyEncounterPortrait), nameof(uiEnemyEncounterPortrait.Initialize))]
        [HarmonyPatch([typeof(FTKPlayerID)])]
        [HarmonyPrefix]
        static void PortraitInitPlayer(FTKPlayerID _pid)
        {
            CharacterOverworld player = FTKHub.Instance.GetCharacterOverworldByFID(_pid);
            involvedPlayers.Add(player);
        }

        public static void CreateEncounterAction(SubPanelBaseBase instance)
        {
            allButtons.Clear();
            MiniHexInfo.MenuPOIDisplayValues values = EncounterMenuInstance.m_ThisMiniHex.GetMenuDisplayValues();
            string ctx = GetEncounterContext(values.m_Title, values.m_Bottom, values.m_Top, EncounterMenuInstance.m_Cost, EncounterMenuInstance.m_EnemyLevel);
            Context.Send(ctx);
            generating = false;
            if (!instance.isActiveAndEnabled) return;
            window = EncounterAction.CreateWindow(instance, activeButtons.ToDictionary(k => k.Key, v => v.Value), buttonsContext);
        }

        // entered combat hex
        [HarmonyPatch(typeof(uiEncounterMenu), "SetMenuPanelMode")] // after getting buttons
        [HarmonyPostfix]
        static void Test3(uiEncounterMenu __instance)
        {
            MiniHexInfo.MiniHexType type = __instance.m_ThisMiniHex.m_MiniHexType;
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.OpenOverworldTreasureChest))]
        [HarmonyPostfix]
        static void Test7()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.OpenOverworldTreasureChest");
        }

        // [HarmonyPatch(typeof(FTKUI), nameof(FTKUI.EnableEncounterMenu))] // call initialize on encounter menu
        // [HarmonyPostfix]
        // static void EncounterMenu()
        // {
        // }

        public static void ResetContextData()
        {
            involvedPlayers = [];
            involvedEnemies = [];
            unitDupeCount = 0;
        }

        /// <summary>
        /// info about encounter, characters involved
        /// </summary>
        /// <returns>"encounter description, characters involved, enemies involved, cost, difficulty, team lvl"</returns>
        public static string GetEncounterContext(string name, string description, string flavor, Text costObj, Text difficultyObj, bool isDungeon = false)
        {
            StringBuilder sb = new($"## Encounter ({name}) {StringReplace.RemoveStyling(flavor)}: {StringReplace.RemoveStyling(description)}\n");
            sb.Append($"- characters involved: ");
            int playerTotalLvl = 0;
            foreach (CharacterOverworld player in involvedPlayers)
            {
                sb.Append($"{CharacterData.GetCharacterName(player)} (lvl {player.m_CharacterStats.m_PlayerLevel}), ");
                playerTotalLvl += player.m_CharacterStats.m_PlayerLevel;
            }
            sb.AppendLine(".");
            int enemyTotalLvl = 0;
            if (involvedEnemies.Count > 0)
            {
                sb.AppendLine($"- enemies involved: {string.Join(", ", [.. involvedEnemies.Select(key => key.Value.Keys.First() + "(lvl " + key.Value.Values.First() + ")")])}.");
                involvedEnemies.Select(x => x.Value.Values.First()).ToList().ForEach(x => enemyTotalLvl += int.TryParse(x, out int lvl) ? lvl : int.TryParse(involvedEnemies.First().Value.Values.First(), out int firstLvl) ? firstLvl : 0); // hidden enemies add visible enemies lvl
            }
            if (costObj.gameObject.activeInHierarchy && costObj.text != string.Empty)
            {
                sb.AppendLine($"- {StringMessages.EncounterCost.Format([costObj.text, CharacterData.GetActiveCow().m_CharacterStats.m_Gold])}.");
            }
            if (difficultyObj.gameObject.activeInHierarchy && difficultyObj.text != string.Empty)
            {
                sb.AppendLine($"- enemy average lvl: {difficultyObj.text}.");
            }
            if ((involvedEnemies.Count > 0 || isDungeon) && involvedPlayers.Count > 0)
            {
                float avg = (float)playerTotalLvl / involvedPlayers.Count;
                sb.Append($"your involved teams average lvl is {avg:F1}.");
                if (isDungeon) enemyTotalLvl = int.TryParse(difficultyObj.text, out int lvl) ? lvl*3 : 0;
                int diff = enemyTotalLvl - playerTotalLvl;
                if (diff > 3 || (involvedEnemies.Count - involvedPlayers.Count) > 1) sb.Append($" This fight will be difficult.");
            }
            return sb.ToString();
        }

        /// <summary>
        /// sets buttonsContext
        /// </summary>
        static bool SetButtonData(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> buttons)
        {
            activeButtons.Clear();
            int dupeCount = 1;
            foreach (KeyValuePair<SubPanelBaseBase.ButtonID, uiPoiButton> kvp in buttons)
            {
                // Plugin.Logger.LogMessage(kvp.Key); // Gamble1
                if (!kvp.Value.isActiveAndEnabled || kvp.Value.m_ButtonLock) continue;
                string text = kvp.Value.m_ButtonText.text; // Play
                if (activeButtons.ContainsKey(text))
                {
                    text += $"_{dupeCount}"; // Play_1
                    dupeCount++;
                }
                activeButtons.Add(text, kvp.Value);
            }
            if (HandleAutoJournal(activeButtons.ToDictionary(k => k.Value.m_ButtonInfo.m_ButtonType, v => v.Value))) return false;
            activeButtons.Remove("Journal");
            Dictionary<string, string> flavorData = [];
            Dictionary<string, object> rollData = [];
            foreach (KeyValuePair<string, uiPoiButton> btn in activeButtons)
            {
                GetButtonData(btn.Key, btn.Value, flavorData, rollData);
            }
            StringBuilder sb = new("this encounters actions displayed as: action - total successful rolls (chance for this result) = outcome result. (actions with no roll results will always succeed)\n");
            foreach (KeyValuePair<string, object> data in rollData)
            {
                // [ambush (ambush flavor)]
                sb.AppendLine($"## {data.Key} ({flavorData[data.Key]})");
                foreach (KeyValuePair<string, Dictionary<string, string>> outcome in (Dictionary<string, Dictionary<string, string>>)data.Value)
                {
                    // 0(2%) = Failure
                    // string value = JsonConvert.SerializeObject(outcome.Value);
                    sb.AppendLine($"- {outcome.Key} ({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}");
                }
            }
            MiniHexInfo.PoiProfile profile = EncounterMenuInstance.m_ThisMiniHex.GetPOIProfile();
            if (profile != null && profile.m_SkillRequired != FTK_weaponStats2.SkillType.none)
            {
                    sb.Append(StringMessages.RollSkillType.Format(ItemData.SwitchSkillTestName(profile.m_SkillRequired)));
            }
            buttonsContext = sb.ToString();
            return true;
        }

        /// <summary>
        /// adds data to dictionaries
        /// <br/> flavorData = key, GameDescriptions.GetEncounterBtnFlavor(id)
        /// <br/> rollData = { "ambush": { 0: {5%: failure} }, { 1: {5%: success} }
        /// </summary>
        static void GetButtonData(string key, uiPoiButton btn, Dictionary<string, string> flavorData, Dictionary<string, object> rollData)
        {
            SubPanelBaseBase.ButtonID id = btn.m_ButtonInfo.m_ButtonType;
            if (flavorData.ContainsKey(key) || rollData.ContainsKey(key))
            {
                Plugin.Logger.LogError($"dupe btn id {id}: {key}");
                return;
            }
            flavorData.Add(key, GameDescriptions.GetEncounterBtnFlavor(id));
            FTK_slotOutput.ID slotId = GetSlotId(btn.m_ButtonInfo.m_ButtonType, CharacterData.GetActiveCow());
            Dictionary<string, Dictionary<string, string>> outcome;
            // ExitFunc means no rolls?
            if (string.IsNullOrEmpty(btn.m_ButtonInfo.m_ExitFunc)) outcome = [];
            else outcome = RollSlotOutcomes.GetOutcomes(CharacterData.GetActiveCow(), slotId);
            rollData.Add(key, outcome);
        }

        public static FTK_slotOutput.ID GetSlotId(SubPanelBaseBase.ButtonID _id, CharacterOverworld cow)
        {
            switch (_id)
            {
                case SubPanelBaseBase.ButtonID.Ambush:
                    return RollSlotOutcomes._getAmbushType((MiniHexEnemy)EncounterMenuInstance.m_ThisMiniHex, cow);
                case SubPanelBaseBase.ButtonID.Sneak:
                    return RollSlotOutcomes._getSneakType((MiniHexEnemy)EncounterMenuInstance.m_ThisMiniHex, cow);
                default:
                    MiniEncounter hex = EncounterMenuInstance.m_ThisMiniHex as MiniEncounter;
                    if (EncounterMenuInstance.m_ActiveSubPanel is uiCarnivalMenu)
                    {
                        switch (_id)
                        {
                            case SubPanelBaseBase.ButtonID.Gamble1:
                                return FTK_slotOutput.GetEnum(_CarnivalOptions[0].m_ID);
                            case SubPanelBaseBase.ButtonID.Gamble2:
                                return FTK_slotOutput.GetEnum(_CarnivalOptions[1].m_ID);
                            case SubPanelBaseBase.ButtonID.Gamble3:
                                return FTK_slotOutput.GetEnum(_CarnivalOptions[2].m_ID);
                        };
                    }
                    else if (hex?.GetDBEntry() != null) return hex.GetDBEntry().m_SlotRoll;
                    break;
            }
            return FTK_slotOutput.ID.None;
        }

        static bool HandleAutoJournal(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> _activeButtons)
        {
            if (_activeButtons.ContainsKey(SubPanelBaseBase.ButtonID.Journal) && !isJournal)
            {
                isJournal = true;
                uiLocationMenuDisplay.Instance.StartCoroutine(ReadJournal(_activeButtons[SubPanelBaseBase.ButtonID.Journal]));
                return true;
            }
            isJournal = false;
            return false;
        }

        static IEnumerator ReadJournal(uiPoiButton btn)
        {
            yield return new WaitForSeconds(0.5f);
            if (btn == null)
            {
                Plugin.Logger.LogError("journal btn is null");
                CreateEncounterAction(EncounterMenuInstance.m_ActiveSubPanel);
                yield break;
            }
            generating = false;
            SelectButton.StartCoroutine(btn, 0.5f);
        }

        #region tests

        [HarmonyPatch(typeof(uiEncounterMenu), "SetEnemyMode")]
        [HarmonyPostfix]
        static void EnemyWindow()
        {
            // Plugin.Logger.LogWarning("enemy encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetDeadAdventurerMode")]
        [HarmonyPostfix]
        static void AdventureWindow()
        {
            // Plugin.Logger.LogWarning("adventurer encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetWishingWellMode")]
        [HarmonyPostfix]
        static void WellWindow()
        {
            // Plugin.Logger.LogWarning("well encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetRevivalMode")]
        [HarmonyPostfix]
        static void ReviveWindow()
        {
            // Plugin.Logger.LogWarning("revive encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetSkillTestMode")]
        [HarmonyPostfix]
        static void SkillWindow()
        {
            // Plugin.Logger.LogWarning("skill encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetServiceMode")]
        [HarmonyPostfix]
        static void ServiceWindow(uiEncounterMenu __instance)
        {
            Plugin.Logger.LogWarning($"service encounter = {__instance.m_ActiveSubPanel.GetType()}");
        }

        #endregion
        
    }
}