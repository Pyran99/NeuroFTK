using HarmonyLib;
using UnityEngine;
using StartGameFE;
using NeuroFTK.GameConfigs;
using System.Collections;
using System.Collections.Generic;
using GridEditor;
using NeuroFTK.HarmonyPatches.AutomatedActions;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;

namespace NeuroFTK.NeuroIntegration.Actions;

[HarmonyPatch]
public class MainMenu
{
    /*
    press new game if not doing auto
    press resume if doing saves (probably)
    lore store items can afford (optional)
    create game
    */

    // if this doesnt work try uiStartGame.ShowStartPage
    [HarmonyPatch(typeof(MainScreen), nameof(MainScreen.Show))]
    [HarmonyPostfix]
    static void OnMainScreenShown(MainScreen __instance)
    {
        //TODO add a wait time before doing anything
        int rand = Random.Range(0, 2);
        if (rand == 0)
        {
            OnNewGameAction(__instance);
        }
        else
        {
            OnResumeGameAction(__instance);
        }
    }

    static void OnNewGameAction(MainScreen __instance)
    {
        Plugin.Logger.LogMessage("new game action");
        // __instance.OnNewGame();
    }

    static void OnResumeGameAction(MainScreen __instance)
    {
        Plugin.Logger.LogMessage("resume game action");
        // __instance.OnResume();
    }

    [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.Show))]
    [HarmonyPostfix]
    static void OnGameConfigShown(GameConfig __instance)
    {
        //TODO PH neuro action for testing
        ActionWindow window = ActionWindow.Create(__instance.gameObject);
        window.SetForce(0, "test action display", "", false, ActionsForce.Priority.Low);
        window.AddAction(new TestAction());
        window.Register();
        // ActionWindow.Create(uiStartGame.Instance.gameObject)
        //     .SetForce(0, "the lore store is showing", "", false, ActionsForce.Priority.Low)
        //     .AddAction(new TestAction())
        //     .Register();

        __instance.StartCoroutine(Wait(__instance));
        static IEnumerator Wait(GameConfig instance)
        {
            yield return new WaitForSeconds(2.0f);
            SelectAdventure(instance);
            yield return new WaitForSeconds(2.0f);
            SetRulesBeforeStartGame(instance);
            yield return new WaitForSeconds(10.0f);
            //TODO change to auto call after closing/skipping house rules
            // instance.OnStartGame();
            Plugin.Logger.LogMessage("start game action " + nameof(instance.OnStartGame));
        }
    }

    static void SetRulesBeforeStartGame(GameConfig instance)
    {
        bool useCustomRules = CustomHouseRules.SET_CUSTOM_RULES;
        if (!useCustomRules) return;
        SetCustomHouseRules.configInstance = instance;
        instance.OnHouseRule();
    }

    static void SelectAdventure(GameConfig instance)
    {
        List<string> names = [];
        instance.m_GameDefButtons.ForEach(btn => names.Add(btn.m_GameDefName));
        // names.ForEach(Plugin.Logger.LogMessage);
        List<string> search = [.. names];
        foreach (string item in search)
        {
            if (FTK_dlcDB.GetDLCBySaveFileName(item) != null)
            {
                if (!FTK_dlcDB.GetDLCBySaveFileName(item).IsPurchased())
                {
                    names.Remove(item);
                }
            }
            if (!CustomHouseRules.houseRules.ContainsKey(item))
            {
                names.Remove(item);
            }
        }
        Plugin.Logger.LogMessage($"valid names: {string.Join(", ", [.. names])}");
        string chosen = names[Random.Range(0, names.Count)];
        if (!GameCache.Cache.GameDefinitions.GetNames().Contains(chosen))
        {
            Plugin.Logger.LogWarning($"could not find game def {chosen}, defaulting to KillVexor");
            chosen = "KillVexor";
        }
        instance.OnChangeValueGameDef(chosen); // default select first -> first is the dev sandbox, dont send empty
        //TODO select the correct button
    }

    [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.OnChangeValueGameDef))]
    [HarmonyPostfix]
    static void OnAdventureSelected(GameConfig __instance)
    {
        // "For the King"
        Plugin.Logger.LogMessage($"selected adventure: {__instance.GetCurrentGameDefPreview().GetDisplayName()}");
        // "KillVexor"
        Plugin.Logger.LogMessage($"selected adventure save file name: {__instance.GetCurrentGameDefPreview().m_SaveFileName}");
    }


    /*
    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    string filePathRelativeToAssembly = Path.Combine(assemblyPath, @"..\SomeFolder\SomeRelativeFile.txt");
    string normalizedPath = Path.GetFullPath(filePathRelativeToAssembly);

    assemblyPath = "C:\Test"
    filePathRelativeToAssembly = "C:\Test\..\SomeFolder\SomeRelativeFile.txt"
    normalizedPath = "C:\Test\SomeFolder\SomeRelativeFile.txt"
    */
}