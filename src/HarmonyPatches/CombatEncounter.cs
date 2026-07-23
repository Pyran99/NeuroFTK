using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
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
        static bool generating = false;
        static ActionWindow window;

        #region patches

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
            Plugin.Logger.LogWarning("subpanel_Generate");
            if (generating) return;
            generating = true;
            instance = __instance.m_Owner;
            ToggleOverworldActions.DisposeActions();
            __instance.StartCoroutine(Wait(__instance.m_Buttons));
        }

        static IEnumerator Wait(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> _buttons)
        {
            // wait for lower class to finish setup
            yield return new WaitForEndOfFrame();
            MiniHexInfo.MenuPOIDisplayValues values = instance.m_ThisMiniHex?.GetMenuDisplayValues();
            string ctx = LocationEncounter.GetEncounterContext(values.m_Title, values.m_Bottom, values.m_Top);
            Context.Send(ctx);
            OnMenuOpened(instance, _buttons);
            QuickTimerCallback timer = new (CreateAction, instance.m_MainPanel.gameObject);
            // Context.Send(EncounterContext(instance.m_PoiName.text, instance.m_LoreDescription.text, instance.m_ThisMiniHex?.GetMenuDisplayValues().m_Top));
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.DisableMenu))]
        [HarmonyPostfix]
        static void DisableMenu()
        {
            Object.Destroy(window);
            ResetData();
            LocationEncounter.ResetContextData();
        }

        [HarmonyPatch(typeof(uiEncounterMenu), nameof(uiEncounterMenu.MenuRefresh))]
        [HarmonyPostfix]
        static void Test2()
        {
            Plugin.Logger.LogMessage("uiEncounterMenu.MenuRefresh");
            SubMenuGenerated(instance.m_ActiveSubPanel);
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

        #endregion

        static void CreateAction()
        {
            generating = false;
            if (!instance.isActiveAndEnabled) return;
            Object.Destroy(window);
            window = EncounterAction.CreateWindow(instance, [.. activeButtons.Values], buttonsContext);
        }

        static void OnMenuOpened(uiEncounterMenu _instance, Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> _buttons)
        {
            instance = _instance;
            SetButtonData(_buttons);
        }

        static void SetButtonData(Dictionary<SubPanelBaseBase.ButtonID, uiPoiButton> buttons)
        {
            activeButtons.Clear();
            foreach (KeyValuePair<SubPanelBaseBase.ButtonID, uiPoiButton> kvp in buttons)
            {
                if (!kvp.Value.isActiveAndEnabled) continue;
                if (activeButtons.ContainsKey(kvp.Key)) continue;
                activeButtons.Add(kvp.Key, kvp.Value);
            }
            Dictionary<string, string> flavorData = [];
            Dictionary<string, object> rollData = [];
            foreach (uiPoiButton btn in activeButtons.Values)
            {
                GetButtonData(btn, flavorData, ref rollData);
            }
            string context = "(this encounters actions (actions with no roll results will always succeed) displayed as: [action (description)] total successful rolls(chance for this result) = outcome result)\n";
            foreach (KeyValuePair<string, object> data in rollData)
            {
                // [ambush (ambush flavor)]
                context += $"[{data.Key} ({flavorData[data.Key]})] \n";
                foreach (KeyValuePair<string, Dictionary<string, string>> outcome in (Dictionary<string, Dictionary<string, string>>)data.Value)
                {
                    // string value = JsonConvert.SerializeObject(outcome.Value);
                    // 0(2%) = Failure
                    context += $"{outcome.Key}({outcome.Value.Keys.First()}) = {outcome.Value.Values.First()}\n";
                }
            }
            MiniHexInfo.PoiProfile profile = instance.m_ThisMiniHex.GetPOIProfile();
            if (profile != null && profile.m_SkillRequired != FTK_weaponStats2.SkillType.none)
            {
                context += $"these roll chances are based on your {profile.m_SkillRequired} stat\n";
            }
            buttonsContext = context;
        }

        /// <summary>
        /// adds data to flavorData and rollData
        /// </summary>
        static void GetButtonData(uiPoiButton btn, Dictionary<string, string> flavorData, ref Dictionary<string, object> rollData)
        {
                if (flavorData.ContainsKey(btn.m_ButtonText.text)) return;
                flavorData.Add(btn.m_ButtonText.text, EncounterButton.GetString(btn.m_ButtonInfo.m_ButtonType));
                FTK_slotOutput.ID id = FTK_slotOutput.ID.None;
                if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Ambush)
                {
                    id = RollSlotOutcomes._getAmbushType((MiniHexEnemy)instance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                else if (btn.m_ButtonInfo.m_ButtonType == SubPanelBaseBase.ButtonID.Sneak)
                {
                    id = RollSlotOutcomes._getSneakType((MiniHexEnemy)instance.m_ThisMiniHex, GameLogic.Instance.GetCurrentCOW());
                }
                if (id == FTK_slotOutput.ID.None)
                {
                    MiniEncounter hex = instance.m_ThisMiniHex as MiniEncounter;
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


        #region may have uses

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


    }
}