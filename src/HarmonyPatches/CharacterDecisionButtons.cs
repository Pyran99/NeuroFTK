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
            activeContainers.Add(__instance);
            if (!Multiplayer.IsYourCow(cow)) return;
            string name = cow.m_CharacterStats.m_CharacterName;
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
            // if (!Multiplayer.IsYourCow(__instance.m_PlayerHud.m_Cow)) return;
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
                activeWindow.AddAction(new CharacterDecisionAction(kvp.Key.m_CharacterStats.m_CharacterName, kvp.Value));
            }
            activeWindow.SetForce(0, $"[{instance.m_Prompt.text}] choose a character to perform the action with. if multiple characters can be chosen, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). collected items can be sold at a market. discard should be avoided for most loot", "");
            StringBuilder sb = new(DungeonEncounterRolls());
            if (sb.Length != 0)
            {
                if (CombatUtils.Entry != null)
                {
                    sb.Append($"these roll chances are based on your {CombatUtils.Entry?.m_TestSkill} stat");
                }
                activeWindow.SetContext(sb.ToString());
            }
            activeWindow.Register();
        }

        static string DungeonEncounterRolls()
        {
            StringBuilder sb = new();
            string detail = "(roll chances (buttons with no roll results will always succeed) displayed as: character [button (description)] total successful rolls(chance for this result) = outcome result)";
            sb.AppendLine(detail);
            foreach (KeyValuePair<CharacterOverworld, List<VoteButton>> kvp in voteButtons)
            {
                CharacterOverworld cow = kvp.Key;
                if (!cow.IsInDungeon()) continue;
                if (cow.m_HexLand.m_POI == null) continue;
                foreach (VoteButton btn in kvp.Value)
                {
                    string btnName = btn.GetComponentInChildren<Text>().text;
                    // if btn text doesnt work
                    // if (GameDescriptions.AlternateLocLookUp.ContainsKey(btn.m_Option.ToString()))
                    // {
                    //     btnName = GameDescriptions.AlternateLocLookUp[btn.m_Option.ToString()];
                    // }
                    sb.AppendLine($"buttons for {CharacterData.GetCharacterName(cow)} [{btnName} ({GameDescriptions.VoteOptionDescriptions[btn.m_Option]})]");
                    string slotResults = CombatUtils.GetDungeonSlotLegend(cow, btn);
                    if (slotResults.Length == 0) continue;
                    sb.AppendLine($"{slotResults}");
                    //expected => Cow [Disarm ()] 0(2%) = Failure
                }
            }
            if (sb.Length == detail.Length) sb = new();
            string encounterMsg = StaticMessage.Message;
            if (encounterMsg.Length != 0)
            {
                sb.Insert(0, $"encountered {StaticMessage.Message}\n");
            }
            return sb.ToString();
        }
    }
}