using HarmonyLib;
using UnityEngine;
using StartGameFE;
using System.Collections;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.GameConfigs;
using System.Collections.Generic;

namespace Pyran.NeuroFTK.HarmonyPatches;

[HarmonyPatch]
public class MainMenu
{
    public static uiFTKButton newGameBtn;
    public static uiFTKButton resumeBtn;
    public static uiFTKButton loreBtn;
    static ActionWindow activeWindow;

    // When the main menu is active
    [HarmonyPatch(typeof(MainScreen), nameof(MainScreen.OnSetFocus))]
    [HarmonyPostfix]
    static void OnSetFocus(MainScreen __instance)
    {
        CharacterDecisionButtons.ResetData();
        GlobalConfig.gameInitialized = false;
        ToggleDisposableActions.ToggleOverworldActions(false);
        ToggleDisposableActions.ToggleCombatActions(false);
        if (GlobalConfig.IsMultiplayer)
        {
            Plugin.Logger.LogWarning("config multiplayer is true, normal action window is disabled");
            return;
        }
        __instance.StartCoroutine(DelayMainMenuAction(__instance));
    }

    static IEnumerator DelayMainMenuAction(MainScreen instance)
    {
        yield return new WaitForSeconds(0.5f);
        FindButtons(instance);
        IEnumerable<string> choices = GetAvailableChoices();
        activeWindow = MainMenuAction.RegisterAction(instance, choices);
        UnregisterDisabledObject.QuickCreate(instance.gameObject, activeWindow);
    }

    static IEnumerable<string> GetAvailableChoices()
    {
        List<string> availableActions = ["new game"];
        if (resumeBtn?.isActiveAndEnabled ?? false) availableActions.Add("resume game");
        if (LoreStoreUnlocks.HasPurchasableLore()) availableActions.Add("spend lore");
        if (GlobalConfig.ForcedCustomAdventure())
        {
            availableActions.Remove("resume game");
        }
        else if (GlobalConfig.ResumeOnFirstLoad() && availableActions.Contains("resume game"))
        {
            availableActions.Remove("new game");
        }
        return availableActions;
    }

    static void FindButtons(MainScreen instance)
    {
        newGameBtn = instance.transform.Find("ButtonRoot/New")?.GetChild(0)?.GetComponent<uiFTKButton>();
        resumeBtn = instance.transform.Find("ButtonRoot/Resume")?.GetChild(0)?.GetComponent<uiFTKButton>();
        loreBtn = instance.transform.Find("ButtonRoot/Lore")?.GetChild(0)?.GetComponent<uiFTKButton>();
    }

    public static void NeuroDecision(string decision)
    {
        switch (decision)
        {
                case "new game":
                    SelectedButton(newGameBtn);
                    break;
                case "resume game":
                    SelectedButton(resumeBtn);
                    break;
                case "spend lore":
                    SelectedButton(loreBtn);
                    break;
                default:
                    Plugin.Logger.LogError($"invalid main menu action '{decision}'");
                    break;
        }
    }

    static void SelectedButton(uiFTKButton button)
    {
        SelectButton.StartCoroutine(button, 1.0f);
    }
}
