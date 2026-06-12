using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class SkipIntroStory
    {
        [HarmonyPatch(typeof(uiStoryIntroCycle), "ShowNextStorySequence")]
        [HarmonyPostfix]
        static void AfterStorySequence(uiStoryIntroCycle __instance, int _index, ref List<string> ___m_StoryEntries)
        {
            Plugin.Logger.LogMessage($"story sequence {_index}");
            Context.Send($"story sequence {_index}: {___m_StoryEntries[_index]}");
            __instance.StartCoroutine(Delay(__instance));

            static IEnumerator Delay(uiStoryIntroCycle __instance)
            {
                yield return new WaitForSeconds(5.0f);
                __instance.FadeNextPage();
            }
        }
    }
}