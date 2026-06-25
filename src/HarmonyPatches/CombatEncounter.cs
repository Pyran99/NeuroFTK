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

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CombatEncounter
    {
        static uiEncounterMenu instance;
        static readonly Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> activeButtons = [];
        static string buttonsContext = "";

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EnableMenu))]
        [HarmonyPostfix]
        static void EncounterMenuEnabled(uiEncounterMenu __instance)
        {
            activeButtons.Clear();
            ToggleOverworldActions.DisableOverworldActions();
            instance = __instance;
            // Type(__instance.m_ThisMiniHex);
            // GetActiveWindow();
            QuickTimerCallback timerCallback = new(CreateAction, 2000f);
            GetContext(__instance.m_PoiName.text, __instance.m_LoreDescription.text);
            //TODO [context1] tasks
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

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void DisableMenu()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.DisableMenu");
            // ToggleOverworldActions.EnableOverworldActions();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.LeaveOrEndTurn))]
        [HarmonyPostfix]
        static void Leave()
        {
            Plugin.Logger.LogMessage("LeaveOrEndTurn");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.EndTurn))]
        [HarmonyPostfix]
        static void EndTurn()
        {
            Plugin.Logger.LogMessage("EndTurn");
        }

        static void Type(MiniHexInfo hex)
        {
            Plugin.Logger.LogWarning($"type: {FTKHub.Localized<TextLore>("STR_" + hex.GetIDString() + "Popup")}");
        }

        static string GetContext(string name, string description)
        {
            // Plugin.Logger.LogMessage($"{instance?.m_ThisMiniHex?.GetMenuDisplayValues().m_Top}");
            return $"[Encounter] {name}: {description}";
        }


        #region menus

        static void GetActiveButtons(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> buttons)
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
                FTK_slotOutput.ID id = FTK_slotOutput.ID.None;
                if (btn.m_ButtonText.text == "Ambush")
                {
                    id = RollSlotOutcomes._getAmbushType((MiniHexEnemy)instance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                else if (btn.m_ButtonText.text == "Sneak")
                {
                    id = RollSlotOutcomes._getSneakType((MiniHexEnemy)instance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                if (id == FTK_slotOutput.ID.None) continue;
                Dictionary<string, Dictionary<string, string>> outcome = RollSlotOutcomes.GetOutcomes(id);
                // { "ambush": { 0: {5%: failure} }, { 1: {5%: success} }
                rollData.Add(btn.m_ButtonText.text, outcome);
            }
            string context = "(this encounters actions displayed as [action (description)] total successful rolls(chance for this result) = outcome result)\n";
            foreach (KeyValuePair<string, object> data in rollData)
            {
                // [ambush](ambush flavor) 
                context += $"[{data.Key} ({flavorData[data.Key]})] \n";
                foreach (KeyValuePair<string, Dictionary<string, string>> outcome in (Dictionary<string, Dictionary<string, string>>)data.Value)
                {
                    // 0(2%) = Failure
                    context += $"{outcome.Key}({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}\n";
                    // string value = JsonConvert.SerializeObject(outcome.Value);
                    // context += $"{outcome.Key} = {value}\n";
                }
            }
            buttonsContext = context;
            // Context.Send(context);
        }

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
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiDeadAdventurerPoiMenu), nameof(uiDeadAdventurerPoiMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void AdventurerPanel(uiDeadAdventurerPoiMenu __instance)
        {
            Plugin.Logger.LogWarning("AdventurerPanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiRevivalMenu), nameof(uiRevivalMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void RevivalPanel(uiRevivalMenu __instance)
        {
            Plugin.Logger.LogWarning("RevivalPanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiWishingWellMenu), nameof(uiWishingWellMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void WishingWellPanel(uiWishingWellMenu __instance)
        {
            Plugin.Logger.LogWarning("WishingWellPanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiSkillTestMenu), nameof(uiSkillTestMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void SkillTestPanel(uiSkillTestMenu __instance)
        {
            Plugin.Logger.LogWarning("SkillTestPanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiServiceMenu), nameof(uiServiceMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void ServicePanel(uiServiceMenu __instance)
        {
            Plugin.Logger.LogWarning("ServicePanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiCarnivalMenu), nameof(uiCarnivalMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void CarnivalPanel(uiCarnivalMenu __instance)
        {
            Plugin.Logger.LogWarning("CarnivalPanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        [HarmonyPatch(typeof(uiGambleDenMenu), nameof(uiGambleDenMenu.GenerateMenu))]
        [HarmonyPostfix]
        static void GamblePanel(uiGambleDenMenu __instance)
        {
            Plugin.Logger.LogWarning("GamblePanel");
            instance = __instance.m_Owner;
            GetActiveButtons(__instance.m_Buttons);
        }

        #endregion



        #region may have uses

        // entered combat hex
        [HarmonyPatch(typeof(uiEncounterMenu), "SetMenuPanelMode")] // may have uses
        [HarmonyPostfix]
        static void Test3(uiEncounterMenu __instance)
        {
            MiniHexInfo.MiniHexType type = __instance.m_ThisMiniHex.m_MiniHexType;
            Plugin.Logger.LogMessage("uiEncounterMenu.SetMenuPanelMode");
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))] // may have uses
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