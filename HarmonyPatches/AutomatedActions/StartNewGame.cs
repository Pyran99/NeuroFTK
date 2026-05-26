using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace NeuroFTK.HarmonyPatches.AutomatedActions
{
    [HarmonyPatch]
    public class StartNewGame
    {
        // in MainMenu neuro actions, can remove later
        [HarmonyPatch(typeof(uiStartGame), nameof(uiStartGame.ShowStartPage))]
        [HarmonyPostfix]
        static void OnMainScreenShow(uiStartGame __instance)
        {
            __instance.StartCoroutine(Wait());

            static IEnumerator Wait()
            {
                yield return new WaitForSeconds(5.0f);
                NewGame();
            }
        }
        
        static void NewGame()
        {
            //TODO send actions to neuro
            Plugin.Logger.LogMessage("Starting new game--REPLACE WITH NEURO ACTION");
            // uiStartGame.Instance.m_MainScreen.OnNewGame();
        }
    }
}