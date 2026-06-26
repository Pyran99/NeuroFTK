using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CombatEncounter
    {
        static uiEncounterMenu instance;
        static readonly Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> activeButtons = [];
        static string buttonsContext = "";
        static readonly List<string> players = [];
        static readonly Dictionary<string, Dictionary<string, string>> enemies = [];
        static int count = 0;

        [HarmonyPatch(typeof(SubPanelBaseBase), nameof(SubPanelBaseBase.GenerateMenu))]
        [HarmonyPrefix]
        static void GenerateMenu()
        {
            activeButtons.Clear();
            players.Clear();
            enemies.Clear();
            count = 0;
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EnableMenu))]
        [HarmonyPostfix]
        static void EncounterMenuEnabled(uiEncounterMenu __instance)
        {
            ToggleOverworldActions.DisableOverworldActions();
            instance = __instance;
            QuickTimerCallback timerCallback = new(CreateAction, 2000f);
            Context.Send(EncounterContext(__instance.m_PoiName.text, __instance.m_LoreDescription.text));
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void DisableMenu()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.DisableMenu");
            GenerateMenu();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.LeaveOrEndTurn))]
        [HarmonyPostfix]
        static void Leave()
        {
            Plugin.Logger.LogMessage("LeaveOrEndTurn");
            ToggleOverworldActions.EnableOverworldActions();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            Plugin.Logger.LogMessage("EndTurn");
        }

        static void CreateAction()
        {
            if (!instance.isActiveAndEnabled) return;
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.AddAction(new EncounterAction(instance, [.. activeButtons.Values]));
            // window.SetForce(3, "choose an action", "you encountered an enemy");
            window.SetContext(buttonsContext);
            window.Register();
        }

        static void Type(MiniHexInfo hex)
        {
            Plugin.Logger.LogWarning($"type: {FTKHub.Localized<TextLore>("STR_" + hex.GetIDString() + "Popup")}");
        }

        static string EncounterContext(string name, string description)
        {
            // 0 {"name","level"}
            string encounter = $"[Encounter] {name}: {description}\n";
            string _players = "";
            if (players.Count > 0)
            {
                _players = $"[characters involved] {string.Join(", ", [.. players.Select(x => x)])}\n";
            }
            string _enemies = "";
            if (enemies.Count > 0)
            {
                _enemies = $"[enemies involved] {string.Join(", ", [.. enemies.Select(key => key.Value.Keys.First() + "(lvl " + key.Value.Values.First() + ")")])}\n";
            }
            return $"{encounter}{_players}{_enemies}";
        }

        [HarmonyPatch(typeof(uiEnemyEncounterPortrait), nameof(uiEnemyEncounterPortrait.Initialize))]
        [HarmonyPatch([typeof(string)])]
        [HarmonyPostfix]
        static void PortraitInitEnemy(string _enemyId)
        {
            Plugin.Logger.LogMessage(_enemyId);
            if (_enemyId.IsNullOrEmpty() || _enemyId == "None")
            {
                Plugin.Logger.LogError("enemy: " + _enemyId);
                return;
            }
            FTK_enemyCombat.ID id = FTK_enemyCombat.GetEnum(_enemyId);
            Plugin.Logger.LogMessage("2 " + id.ToString());
            FTK_enemyCombat entry = FTK_enemyCombatDB.Get(id);
            Plugin.Logger.LogMessage("3 " + entry.m_ID);
            string lvl = "";
            if (id != FTK_enemyCombat.ID.None && HauntManager.IsScourgeActive(HauntManager.Scourge.Deimos) && entry.CanBeRandomized())
            {
                id = FTK_enemyCombat.ID.None;
            }
            if (id != FTK_enemyCombat.ID.None)
            {
                lvl = entry.GetEnemyLevelDisplay().ToString();
            }
            enemies[count.ToString()] = new()
            {
                {entry.GetEnemyDisplay(), lvl},
            };
            count++;
        }

        [HarmonyPatch(typeof(uiEnemyEncounterPortrait), nameof(uiEnemyEncounterPortrait.Initialize))]
        [HarmonyPatch([typeof(FTKPlayerID)])]
        [HarmonyPostfix]
        static void PortraitInitPlayer(FTKPlayerID _pid)
        {
            CharacterOverworld player = FTKHub.Instance.GetCharacterOverworldByFID(_pid);
            players.Add(player.m_CharacterStats.m_CharacterName);
        }

        static void SetButtonData(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> buttons)
        {
            activeButtons.Clear();
            Plugin.Logger.LogMessage($"all btns: {string.Join(", ", [.. buttons.Select(x => x.Value.m_ButtonText.text)])}");
            foreach (KeyValuePair<SubPanelBaseBase.ButtonID, uiPoiButton> kvp in buttons)
            {
                if (!kvp.Value.isActiveAndEnabled) continue;
                if (activeButtons.ContainsKey(kvp.Key)) continue;
                activeButtons.Add(kvp.Key, kvp.Value);
            }
            Plugin.Logger.LogMessage($"active buttons: {string.Join(", ", [.. activeButtons.Select(x => x.Value.m_ButtonText.text)])}");
            Dictionary<string, string> flavorData = [];
            Dictionary<string, object> rollData = [];
            foreach (uiPoiButton btn in activeButtons.Values)
            {
                if (flavorData.ContainsKey(btn.m_ButtonText.text)) continue;
                flavorData.Add(btn.m_ButtonText.text, EncounterButtonFlavor.GetString(btn.m_ButtonInfo.m_ButtonType));
// this.m_Owner.m_SlotPanel.PrepareSlots(base.CurrentCow, base.ThisHex().GetDBEntry().m_SlotRoll, base.ThisHex().m_SkillRoll, true);
                FTK_slotOutput slot = FTK_slotOutputDB.GetDB().GetEntry((instance.m_ThisMiniHex as MiniEncounter).GetDBEntry().m_SlotRoll);
                FTK_slotOutput.ID id = FTK_slotOutput.ID.None;
                // FTK_slotOutput.ID id = FTK_slotOutput.ID.None; //FIXME
                // if (btn.m_ButtonText.text == "Ambush")
                if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Ambush)
                {
                    id = RollSlotOutcomes._getAmbushType((MiniHexEnemy)instance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                else if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Sneak)
                {
                    id = RollSlotOutcomes._getSneakType((MiniHexEnemy)instance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                if (id == FTK_slotOutput.ID.None) continue;
                Dictionary<string, Dictionary<string, string>> outcome = RollSlotOutcomes.GetOutcomes(id);
                // { "ambush": { 0: {5%: failure} }, { 1: {5%: success} }
                rollData.Add(btn.m_ButtonText.text, outcome);
            }
            string context = "(this encounters actions (actions not shown here will always succeed) displayed as [action (description)] total successful rolls(chance for this result) = outcome result)\n";
            foreach (KeyValuePair<string, object> data in rollData)
            {
                // [ambush](ambush flavor) 
                context += $"[{data.Key} ({flavorData[data.Key]})] \n";
                foreach (KeyValuePair<string, Dictionary<string, string>> outcome in (Dictionary<string, Dictionary<string, string>>)data.Value)
                {
                    // string value = JsonConvert.SerializeObject(outcome.Value);
                    // 0(2%) = Failure
                    context += $"{outcome.Key}({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}\n";
                }
            }
            buttonsContext = context;
        }


        #region menus

        static FTK_slotOutput.ID slotID = FTK_slotOutput.ID.None;

        // alternate way to get the active window. unsure how to specify the type
        static void GetActiveWindow()
        {
            SubPanelBaseBase subPanelBase = null;
            GameObject first = instance?.transform.Find("MainPanel")?.gameObject;
            GameObject menu = first?.transform.Find("MenuPanel")?.gameObject;
            GameObject slots = menu?.transform.Find("SlotsAndSubPanels")?.gameObject;
            GameObject subMenu = slots?.transform.Find("SubPanels")?.gameObject;
            if (subMenu == null) return;
            foreach (Transform child in subMenu.transform)
            {
                Plugin.Logger.LogMessage(child.name);
                if (child.gameObject.activeInHierarchy)
                {
                    Plugin.Logger.LogWarning($"encounter = {child.name}");
                    subPanelBase = child.GetComponent<SubPanelBaseBase>();
                    break;
                }
            }
            Plugin.Logger.LogWarning(subPanelBase.gameObject.name);
        }

        [HarmonyPatch(typeof(uiEnemyPoiMenu), nameof(uiEnemyPoiMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void EnemyPanel(uiEnemyPoiMenu __instance)
        {
            Plugin.Logger.LogWarning("EnemyPanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiDeadAdventurerPoiMenu), nameof(uiDeadAdventurerPoiMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void AdventurerPanel(uiDeadAdventurerPoiMenu __instance)
        {
            Plugin.Logger.LogWarning("AdventurerPanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiRevivalMenu), nameof(uiRevivalMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void RevivalPanel(uiRevivalMenu __instance)
        {
            Plugin.Logger.LogWarning("RevivalPanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiWishingWellMenu), nameof(uiWishingWellMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void WishingWellPanel(uiWishingWellMenu __instance)
        {
            Plugin.Logger.LogWarning("WishingWellPanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        public static class EncounterMenuType<T> // TODO maybe way of storing each __instance
        {
            public static T test;
        }

        [HarmonyPatch(typeof(uiSkillTestMenu), nameof(uiSkillTestMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void SkillTestPanel(uiSkillTestMenu __instance)
        {
            Plugin.Logger.LogWarning("SkillTestPanel"); //TODO
            instance = __instance.m_Owner;
            // FTK_slotOutput slot = FTK_slotOutputDB.Get(__instance.ThisHex().GetDBEntry().m_SlotRoll);
            FTK_slotOutput.ID id = __instance.ThisHex().GetDBEntry().m_SlotRoll;
            EncounterMenuType<uiSkillTestMenu>.test = __instance; // testing
            // FTK_slotOutput.ID id = FTK_slotOutput.GetEnum(slot.m_ID);
            Plugin.Logger.LogWarning($"id: {id}");
            FTK_weaponStats2.SkillType skillType = __instance.ThisHex().m_SkillRoll;
            Plugin.Logger.LogWarning($"skill: {skillType}");
            SetButtonData(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiServiceMenu), nameof(uiServiceMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void ServicePanel(uiServiceMenu __instance)
        {
            Plugin.Logger.LogWarning("ServicePanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiCarnivalMenu), nameof(uiCarnivalMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void CarnivalPanel(uiCarnivalMenu __instance)
        {
            Plugin.Logger.LogWarning("CarnivalPanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiGambleDenMenu), nameof(uiGambleDenMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void GamblePanel(uiGambleDenMenu __instance)
        {
            Plugin.Logger.LogWarning("GamblePanel");
            instance = __instance.m_Owner;
            SetButtonData(__instance.m_Buttons);
        }

        #endregion



        #region may have uses

        // entered combat hex
        [HarmonyPatch(typeof(uiEncounterMenu), "SetMenuPanelMode")] // after getting buttons
        [HarmonyPostfix]
        static void Test3(uiEncounterMenu __instance)
        {
            MiniHexInfo.MiniHexType type = __instance.m_ThisMiniHex.m_MiniHexType;
            Plugin.Logger.LogMessage("uiEncounterMenu.SetMenuPanelMode");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))] // after panel mode
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