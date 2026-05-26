using HarmonyLib;
using UnityEngine;
using StartGameFE;
using NeuroFTK.GameConfigs;
using System.Collections;
using System.Collections.Generic;
using GridEditor;

namespace NeuroFTK.NeuroIntegration.Actions
{
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
            int rand = Random.Range(0, 1);
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
            __instance.StartCoroutine(Wait(__instance));
            static IEnumerator Wait(GameConfig instance)
            {
                yield return new WaitForSeconds(2.0f);
                SelectAdventure(instance);
                yield return new WaitForSeconds(2.0f);
                SetRulesBeforeStartGame(instance);
                yield return new WaitForSeconds(10.0f);
                // instance.OnStartGame();
                Plugin.Logger.LogMessage("start game action " + nameof(instance.OnStartGame));
            }
        }

        static void SetRulesBeforeStartGame(GameConfig instance)
        {
            if (!CustomHouseRules.SET_CUSTOM_RULES) return;
            instance.OnHouseRule(); // will this wait? NOPE
        }

        static void SelectAdventure(GameConfig instance)
        {
            // names: 1=KillVexor 2=FrostAdventure 3=Pirates 4=DungeonCrawl 5=HildebrantsCellar 6=GraveRobber 7=LostCiv -> DLC
            Plugin.Logger.LogMessage("--adventure buttons--");
            List<string> names = [];
            instance.m_GameDefButtons.ForEach(btn => names.Add(btn.m_GameDefName));
            names.ForEach(Plugin.Logger.LogMessage);
            Plugin.Logger.LogMessage($"names joined list: {string.Join(", ", [.. names])}");
            List<string> search = [.. names];
            foreach (string item in search) // Collection was modified; enumeration operation may not execute.
            {
                if (FTK_dlcDB.GetDLCBySaveFileName(item) != null)
                {
                    if (!FTK_dlcDB.GetDLCBySaveFileName(item).IsPurchased())
                    {
                        names.Remove(item);
                    }
                }
            }
            Plugin.Logger.LogMessage($"valid names: {string.Join(", ", [.. names])}");
            instance.OnChangeValueGameDef("KillVexor"); // default select first -> first is the dev sandbox, dont send empty
        }
        
    }
}