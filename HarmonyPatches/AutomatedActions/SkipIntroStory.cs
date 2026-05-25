using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace NeuroFTK.HarmonyPatches.AutomatedActions
{
    [HarmonyPatch]
    public class SkipIntroStory
    {
        [HarmonyPatch(typeof(uiStoryIntroCycle), "ShowNextStorySequence")]
        [HarmonyPostfix]
        static void AfterStorySequence(uiStoryIntroCycle __instance, int _index)
        {
            Plugin.Logger.LogMessage($"story sequence {_index}");
            //TODO send neuro context of: __instance.m_TextBody.text or __instance.m_StoryEntries[_index]
            // __instance.GoIntoGame(); // maybe immediate skip -- 
            Delay(__instance);

            static IEnumerator Delay(uiStoryIntroCycle __instance)
            {
                yield return new WaitForSeconds(2.0f);
                __instance.FadeNextPage();
            }
        }

        // missing assembly
        // static void ImmediateSkip(uiStoryIntroCycle __instance)
        // {
        //     new FTKUI.ScreenFadeInfo(0f, 1f, 0.5f, new ContinueFSM(new Action(__instance.GoIntoGame), ContinueFSM.WaitClients.Self));
        // }
        
    }
}