using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class CharacterDecisionButtons
    {
        static bool isShowing = false;
        static readonly Dictionary<string, List<string>> data = [];
        public static readonly Dictionary<string, List<VoteButton>> voteButtons = [];
        public static VoteButtonContainer instance;

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.ShowDeciding))]
        [HarmonyPostfix]
        static void Test(VoteButtonContainer __instance)
        {
            Plugin.Logger.LogMessage("1 VoteContainerShowDeciding Text: " + __instance.m_Prompt.text);
        }

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Show))] // called for each available character
        [HarmonyPostfix]
        static void Test2(VoteButtonContainer __instance)
        {
            string name = __instance.m_PlayerHud.m_Cow.m_CharacterStats.m_CharacterName;
            data[name] = [];
            voteButtons[name] = [];
            Button[] btns = __instance.GetComponentsInChildren<Button>();
            foreach (Button btn in btns)
            {
                VoteButton voteButton = btn.GetComponent<VoteButton>();
                if (voteButton != null)
                {
                    // Collect | Pass (discard)
                    data[name].Add(voteButton.m_Option.ToString());
                    voteButtons[name].Add(voteButton);
                }
            }

            if (isShowing) return;
            isShowing = true;
            instance = __instance;
            Plugin.Logger.LogMessage("2 VoteContainerShow Text: " + __instance.m_Prompt.text);
            System.Timers.Timer timer = new(1000)
            {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => CreateAction();
            timer.Start();
        }

        static void CreateAction()
        {
            ActionWindow window = ActionWindow.Create(Plugin.Instance.gameObject);
            foreach (KeyValuePair<string, List<string>> kvp in data)
            {
                window.AddAction(new CharacterDecisionAction(kvp.Key, kvp.Value));
            }
            // window.AddAction(new CharacterDecisionAction(data));
            window.SetContext("if multiple characters can decide on an item, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). collected items can be sold at a market");
            window.Register();
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
        static void Test3(VoteButtonContainer __instance)
        {
            Plugin.Logger.LogMessage("3 VoteContainerHide");
            data.Clear();
            isShowing = false;
        }

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.RefreshVoteButtons))] // calls both hide & show
        [HarmonyPatch([])]
        [HarmonyPostfix]
        static void Test4()
        {
            Plugin.Logger.LogMessage("4 VoteContainerRefresh");
            isShowing = false;
        }

        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.DisplayLootItem))]
        [HarmonyPostfix]
        static void Test47(string _item)
        {
            //TODO send as context for current decision
            Plugin.Logger.LogMessage("47 DisplayLootItem: " + _item);
        }

        // shows for belt items, but not inventory?
        // [HarmonyPatch(typeof(uiPlayerMainHud), nameof(uiPlayerMainHud.ShowItemCard))]
        // [HarmonyPostfix]
        // static void Test6(FTK_itembase.ID _itemID, CharacterOverworld _cow)
        // {
        //     Plugin.Logger.LogMessage($"6 ShowItemCard || {_itemID} || {_cow.m_CharacterStats.m_CharacterName}");
        // }

        // [HarmonyPatch(typeof(uiPlayerMainHud), nameof(uiPlayerMainHud.CloseItemCard))]
        // [HarmonyPostfix]
        // static void Test7()
        // {
        //     Plugin.Logger.LogMessage($"7 CloseItemCard");
        // }
    }
}