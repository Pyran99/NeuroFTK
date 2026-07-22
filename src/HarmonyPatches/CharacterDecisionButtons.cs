using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static readonly Dictionary<CharacterOverworld, List<VoteButton>> voteButtons = [];
        public static VoteButtonContainer instance;
        static ActionWindow activeWindow;

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Show))] // called for each available character
        [HarmonyPostfix]
        static void VoteContainerShow(VoteButtonContainer __instance)
        {
            if (!Multiplayer.IsOwnerTurn(__instance.m_PlayerHud.m_Cow)) return;
            CharacterOverworld cow = __instance.m_PlayerHud.m_Cow;
            string name = cow.m_CharacterStats.m_CharacterName;
            Plugin.Logger.LogWarning("decision bug14 checking: " + name); // bug 14
            voteButtons[cow] = [];
            Button[] btns = __instance.GetComponentsInChildren<Button>();
            foreach (Button btn in btns)
            {
                VoteButton voteButton = btn.GetComponent<VoteButton>();
                if (voteButton != null)
                {
                    voteButtons[cow].Add(voteButton);
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
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                activeWindow.AddAction(new CharacterDecisionAction(kvp.Key.m_CharacterStats.m_CharacterName, kvp.Value));
            }
            activeWindow.SetForce(0, $"[{instance.m_Prompt.text}] choose a character to perform the action with. if multiple characters can be chosen, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). collected items can be sold at a market. discard should be avoided for most loot", "");
            //TODO dungeon encounter roll chances as context here?
            StringBuilder sb = new(DungeonEncounterRolls());
            if (sb.Length != 0)
            {
                sb.Append($"these roll chances are based on your (NYI) stat");
                activeWindow.SetContext(sb.ToString());
            }
            activeWindow.Register();
        }

        static string DungeonEncounterRolls()
        {
            StringBuilder sb = new();
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                CharacterOverworld cow = kvp.Key;
                if (!cow.IsInDungeon()) continue;
                if (cow.m_HexLand.m_POI == null) continue;
                foreach (VoteButton btn in kvp.Value)
                {
                    // [Disarm ()]
                    sb.AppendLine($"[{btn.m_Option} ({GameDescriptions.VoteOptionDescriptions[btn.m_Option]})]");
                    // 0(2%) = Failure
                    sb.AppendLine($"{CombatUtils.GetDungeonSlotLegend(cow, btn.m_Option)}");
                }
            }
            if (sb.Length == 0) return "";
            sb.Insert(0, "(dungeon encounter rolls (actions with no roll results will always succeed) displayed as: [action (description)] total successful rolls(chance for this result) = outcome result)\n");
            Plugin.Logger.LogWarning(sb.ToString());
            return sb.ToString();
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