using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CharacterDecisionButtons
    {
        static bool isShowing = false;
        // {character: valid buttons}
        public static readonly Dictionary<string, List<VoteButton>> voteButtons = [];
        public static VoteButtonContainer instance;
        static ActionWindow activeWindow;

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Show))] // called for each available character
        [HarmonyPostfix]
        static void VoteContainerShow(VoteButtonContainer __instance)
        {
            string name = __instance.m_PlayerHud.m_Cow.m_CharacterStats.m_CharacterName;
            voteButtons[name] = [];
            Button[] btns = __instance.GetComponentsInChildren<Button>();
            foreach (Button btn in btns)
            {
                VoteButton voteButton = btn.GetComponent<VoteButton>();
                if (voteButton != null)
                {
                    voteButtons[name].Add(voteButton);
                }
            }

            if (isShowing) return;
            isShowing = true;
            instance = __instance;
            Object.Destroy(activeWindow);
            System.Timers.Timer timer = new(1000)
            {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => CreateAction();
            timer.Start();
        }

        static void CreateAction()
        {
            activeWindow = ActionWindow.Create(instance.gameObject);
            foreach (KeyValuePair<string, List<VoteButton>> kvp in voteButtons)
            {
                activeWindow.AddAction(new CharacterDecisionAction(kvp.Key, kvp.Value));
            }
            activeWindow.SetContext($"[{instance.m_Prompt.text}] if multiple characters can be chosen, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). collected items can be sold at a market");
            activeWindow.SetForce(0, "choose a character to perform an action with", "");
            activeWindow.Register();
        }

        [HarmonyPatch(typeof(VoteButtonContainer), "_showFadeIn")] // not called
        [HarmonyPostfix]
        static System.Collections.IEnumerator FadeIn(System.Collections.IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
            Plugin.Logger.LogWarning("voteContainerShowFadeIn");
        }

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Hide))]
        [HarmonyPrefix]
        static void VoteContainerHide(VoteButtonContainer __instance)
        {
            voteButtons.Clear();
            isShowing = false;
            Object.Destroy(activeWindow);
        }

        // [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.RefreshVoteButtons))] // calls both hide & show
        // [HarmonyPatch([])]
        // [HarmonyPostfix]
        // static void VoteContainerRefresh()
        // {
        //     isShowing = false;
        // }

    }
}