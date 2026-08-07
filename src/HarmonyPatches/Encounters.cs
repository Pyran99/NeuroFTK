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
using WebSocketSharp;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class Encounters
    {
        static ActionWindow window;
        public static uiEncounterMenu encounterMenuInstance { get; private set; }
        public static List<CharacterOverworld> involvedPlayers = [];
        public static Dictionary<string, Dictionary<string, string>> involvedEnemies = [];

        static readonly Dictionary<string, uiPoiButton> activeButtons = [];
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
            Plugin.Logger.LogMessage("encounter type = " + __instance.GetType());
            if (Multiplayer.IsMultiplayer())
            {
                if (!Multiplayer.IsYourCow(__instance.CurrentCow)) return;
            }
            generating = true;
            encounterMenuInstance = __instance.m_Owner;
            __instance.StartCoroutine(Wait(__instance.m_Buttons));

            static IEnumerator Wait(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> _buttons)
            {
                // wait for lower class to finish setup
                Object.Destroy(window);
                yield return null;
                if (!SetButtonData(_buttons)) yield break;
                QuickTimerCallback timer = new (() => CreateEncounterAction(encounterMenuInstance.m_ActiveSubPanel), encounterMenuInstance.m_MainPanel.gameObject);
                // Context.Send(EncounterContext(instance.m_PoiName.text, instance.m_LoreDescription.text, instance.m_ThisMiniHex?.GetMenuDisplayValues().m_Top));
            }
        }

        [HarmonyPatch(typeof(uiCarnivalMenu), "CreateCarnivalOptions")]
        [HarmonyPostfix]
        static void CarnivalOptions(ref List<FTK_slotOutput> ___m_CarnivalOptions)
        {
            Plugin.Logger.LogMessage("set carnival options");
            _CarnivalOptions = [.. ___m_CarnivalOptions];
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
            SubMenuGenerated(encounterMenuInstance.m_ActiveSubPanel);
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
            MiniHexInfo.MenuPOIDisplayValues values = encounterMenuInstance.m_ThisMiniHex.GetMenuDisplayValues();
            string ctx = GetEncounterContext(values.m_Title, values.m_Bottom, values.m_Top);
            Context.Send(ctx);
            generating = false;
            if (!instance.isActiveAndEnabled) return;
            window = EncounterAction.CreateWindow(instance, activeButtons.ToDictionary(k => k.Key, v => v.Value), buttonsContext);
            // window = EncounterAction.CreateWindow(encounterMenuInstance, [.. activeButtons.Values], buttonsContext);
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
        /// <returns>"{encounter}\n{_players}\n{_enemies}"</returns>
        public static string GetEncounterContext(string name, string description, string flavor)
        {
            string encounter = $"[Encounter] ({name}) {StringReplace.RemoveStyling(flavor)}; {StringReplace.RemoveStyling(description)}\n";
            StringBuilder sbPlayers = new("[character involved]");
            foreach (CharacterOverworld player in involvedPlayers)
            {
                sbPlayers.AppendLine($"{CharacterData.GetCharacterName(player)} (lvl {player.m_CharacterStats.m_PlayerLevel})");
            }
            string _enemies = "";
            if (involvedEnemies.Count > 0)
            {
                _enemies = $"[enemies involved] {string.Join(", ", [.. involvedEnemies.Select(key => key.Value.Keys.First() + "(lvl " + key.Value.Values.First() + ")")])}";
            }
            string cost = "";
            if (encounterMenuInstance.m_CostRoot.gameObject.activeInHierarchy && encounterMenuInstance.m_Cost.text != string.Empty)
            {
                cost = $"\n[encounter cost] {encounterMenuInstance.m_Cost?.text} gold";
            }
            string enemyLvl = "";
            if (encounterMenuInstance.m_EnemyLevelRoot.gameObject.activeInHierarchy && encounterMenuInstance.m_EnemyLevel.text != string.Empty)
            {
                enemyLvl = $"\n[enemy level] {encounterMenuInstance.m_EnemyLevel.text}";
            }
            return $"{encounter}{sbPlayers}{_enemies}{cost}{enemyLvl}";
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
            Dictionary<string, string> flavorData = [];
            Dictionary<string, object> rollData = [];
            foreach (KeyValuePair<string, uiPoiButton> btn in activeButtons)
            {
                GetButtonData(btn.Key, btn.Value, flavorData, rollData);
            }
            StringBuilder sb = new("this encounters actions displayed as: [action ()] total successful rolls (chance for this result) = outcome result. (actions with no roll results will always succeed)\n");
            foreach (KeyValuePair<string, object> data in rollData)
            {
                // [ambush (ambush flavor)]
                sb.AppendLine($"[{data.Key} ({flavorData[data.Key]})]");
                foreach (KeyValuePair<string, Dictionary<string, string>> outcome in (Dictionary<string, Dictionary<string, string>>)data.Value)
                {
                    // string value = JsonConvert.SerializeObject(outcome.Value);
                    // 0(2%) = Failure
                    sb.AppendLine($"{outcome.Key} ({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}");
                }
            }
            MiniHexInfo.PoiProfile profile = encounterMenuInstance.m_ThisMiniHex.GetPOIProfile();
            if (profile != null && profile.m_SkillRequired != FTK_weaponStats2.SkillType.none)
            {
                sb.Append($"these roll chances are based on your {profile.m_SkillRequired} stat");
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
            FTK_slotOutput.ID slotId = FTK_slotOutput.ID.None;
            if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Ambush)
            {
                slotId = RollSlotOutcomes._getAmbushType((MiniHexEnemy)encounterMenuInstance.m_ThisMiniHex, CharacterData.GetNeuroCow());
            }
            else if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Sneak)
            {
                slotId = RollSlotOutcomes._getSneakType((MiniHexEnemy)encounterMenuInstance.m_ThisMiniHex, CharacterData.GetNeuroCow());
            }
            if (slotId == FTK_slotOutput.ID.None)
            {
                MiniEncounter hex = encounterMenuInstance.m_ThisMiniHex as MiniEncounter;
                if (encounterMenuInstance.m_ActiveSubPanel is uiCarnivalMenu)
                {
                    switch (btn.m_ButtonInfo.m_ButtonType)
                    {
                        case SubPanelBaseBase.ButtonID.Gamble1:
                            slotId = FTK_slotOutput.GetEnum(_CarnivalOptions[0].m_ID);
                            break;
                        case SubPanelBaseBase.ButtonID.Gamble2:
                            slotId = FTK_slotOutput.GetEnum(_CarnivalOptions[1].m_ID);
                            break;
                        case SubPanelBaseBase.ButtonID.Gamble3:
                            slotId = FTK_slotOutput.GetEnum(_CarnivalOptions[2].m_ID);
                            break;
                    };
                }
                else if (hex?.GetDBEntry() != null)
                {
                    slotId = hex.GetDBEntry().m_SlotRoll;
                }
            }
            Dictionary<string, Dictionary<string, string>> outcome;
            // ExitFunc means no rolls?
            if (string.IsNullOrEmpty(btn.m_ButtonInfo.m_ExitFunc))
            {
                outcome = [];
            }
            else
            {
                outcome = RollSlotOutcomes.GetOutcomes(CharacterData.GetNeuroCow(), slotId);
            }
            rollData.Add(key, outcome);
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
            _activeButtons.Remove(SubPanelBaseBase.ButtonID.Journal);
            return false;
        }

        static IEnumerator ReadJournal(uiPoiButton btn)
        {
            yield return new WaitForSeconds(0.5f);
            if (btn == null)
            {
                Plugin.Logger.LogError("journal btn is null");
                CreateEncounterAction(encounterMenuInstance.m_ActiveSubPanel);
                yield break;
            }
            SelectButton.StartCoroutine(btn, 0.5f);
        }

        #region tests

        [HarmonyPatch(typeof(uiEncounterMenu), "SetEnemyMode")]
        [HarmonyPostfix]
        static void EnemyWindow()
        {
            Plugin.Logger.LogWarning("enemy encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetDeadAdventurerMode")]
        [HarmonyPostfix]
        static void AdventureWindow()
        {
            Plugin.Logger.LogWarning("adventurer encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetWishingWellMode")]
        [HarmonyPostfix]
        static void WellWindow()
        {
            Plugin.Logger.LogWarning("well encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetRevivalMode")]
        [HarmonyPostfix]
        static void ReviveWindow()
        {
            Plugin.Logger.LogWarning("revive encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetSkillTestMode")]
        [HarmonyPostfix]
        static void SkillWindow()
        {
            Plugin.Logger.LogWarning("skill encounter");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), "SetServiceMode")]
        [HarmonyPostfix]
        static void ServiceWindow()
        {
            Plugin.Logger.LogWarning("service encounter");
        }

        #endregion
        
    }
}