using System.Collections.Generic;
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
        static readonly List<VoteButtonContainer> activeContainers = [];

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Show))] // called for each available character
        [HarmonyPostfix]
        static void VoteContainerShow(VoteButtonContainer __instance)
        {
            CharacterOverworld cow = __instance.m_PlayerHud.m_Cow;
            if (!Multiplayer.IsYourCow(cow)) return;
            activeContainers.Add(__instance);
            string name = CharacterData.GetCharacterName(cow);
            voteButtons[cow] = [];
            VoteButton[] btns = __instance.GetComponentsInChildren<VoteButton>();
            foreach (VoteButton btn in btns)
            {
                if (btn != null)
                {
                    voteButtons[cow].Add(btn);
                }
            }
            if (isShowing) return;
            isShowing = true;
            instance = __instance;
            Object.Destroy(activeWindow);
            QuickTimerCallback timer = new(CreateAction, __instance.gameObject);
        }

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Hide))]
        [HarmonyPrefix]
        static void VoteContainerHide(VoteButtonContainer __instance)
        {
            if (activeContainers.Contains(__instance)) activeContainers.Remove(__instance);
            if (activeContainers.Count > 0) return;
            voteButtons.Clear();
            isShowing = false;
            instance = null;
            Object.Destroy(activeWindow);
        }


        static void CreateAction()
        {
            activeWindow = ActionWindow.Create(instance.gameObject);
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                activeWindow.AddAction(new CharacterDecisionAction(CharacterData.GetCharacterName(kvp.Key), kvp.Value));
            }
            activeWindow.SetForce(0, StringMessages.DecisionButtonsPrompt.Format(instance.m_Prompt.text), "");
            StringBuilder sb = new(DungeonEncounterRolls());
            if (sb.Length != 0)
            {
                if (CombatUtils.Entry != null)
                {
                    sb.Append(StringMessages.RollSkillType.Format(CombatUtils.Entry.m_TestSkill.ToString()));
                }
                activeWindow.SetContext(sb.ToString());
            }
            activeWindow.Register();
        }

        static string DungeonEncounterRolls()
        {
            StringBuilder sb = new();
            string detail = StringMessages.DungeonRolls;
            sb.AppendLine(detail);
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                CharacterOverworld cow = kvp.Key;
                sb.AppendLine($"### {CharacterData.GetCharacterName(cow)}");
                foreach (VoteButton btn in kvp.Value)
                {
                    string btnName = btn.GetComponentInChildren<Text>().text;
                    // if btn text doesnt work
                    // if (GameDescriptions.AlternateLocLookUp.ContainsKey(btn.m_Option.ToString())) btnName = GameDescriptions.AlternateLocLookUp[btn.m_Option.ToString()];
                    sb.AppendLine($"#### {btnName} ({GameDescriptions.VoteOptionDescriptions[btn.m_Option]})"); // alternate
                    string slotResults = CombatUtils.GetDungeonSlotLegend(cow, btn);
                    if (slotResults.Length == 0) continue;
                    sb.AppendLine($"{slotResults}");
                    //expected => ### Cow #### Disarm (desc) - 0(2%) = Failure
                }
            }
            string encounterMsg = StaticMessage.Message;
            if (encounterMsg.Length != 0)
            {
                sb.Insert(0, $"## encountered {StaticMessage.Message}\n");
            }
            return sb.ToString().TrimEnd(['\n']);
        }
    }
}