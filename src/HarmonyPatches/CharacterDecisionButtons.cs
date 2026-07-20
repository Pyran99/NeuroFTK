using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
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
            if (!Multiplayer.IsOwnerTurn(__instance.m_PlayerHud.m_Cow)) return;
            string name = __instance.m_PlayerHud.m_Cow.m_CharacterStats.m_CharacterName;
            Plugin.Logger.LogWarning("decision: " + name); // bug 14
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
            QuickTimerCallback timer = new(CreateAction, __instance.m_Prompt.gameObject);
        }

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Hide))]
        [HarmonyPrefix]
        static void VoteContainerHide(VoteButtonContainer __instance)
        {
            voteButtons.Clear();
            isShowing = false;
            Object.Destroy(activeWindow);
        }

        static void CreateAction()
        {
            activeWindow = ActionWindow.Create(instance.gameObject);
            foreach (KeyValuePair<string, List<VoteButton>> kvp in voteButtons)
            {
                activeWindow.AddAction(new CharacterDecisionAction(kvp.Key, kvp.Value));
            }
            activeWindow.SetForce(0, $"[{instance.m_Prompt.text}] choose a character to perform the action with. if multiple characters can be chosen, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). collected items can be sold at a market. discard should be avoided for most loot", "");
            activeWindow.Register();
        }

        // [HarmonyPatch(typeof(VoteButtonContainer), "_showFadeIn")] // not called
        // [HarmonyPostfix]
        // static System.Collections.IEnumerator FadeIn(System.Collections.IEnumerator __result)
        // {
        //     while (__result.MoveNext()) yield return __result.Current;
        //     Plugin.Logger.LogWarning("voteContainerShowFadeIn");
        // }

        // [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.RefreshVoteButtons))] // calls both hide & show
        // [HarmonyPatch([])]
        // [HarmonyPostfix]
        // static void VoteContainerRefresh()
        // {
        //     isShowing = false;
        // }

    }
}