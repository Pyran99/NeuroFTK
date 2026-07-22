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

        [HarmonyPatch(typeof(VoteButtonContainer), nameof(VoteButtonContainer.Show))] // called for each available character
        [HarmonyPostfix]
        static void VoteContainerShow(VoteButtonContainer __instance)
        {
            if (!Multiplayer.IsOwnerTurn(__instance.m_PlayerHud.m_Cow)) return;
            CharacterOverworld cow = __instance.m_PlayerHud.m_Cow;
            string name = cow.m_CharacterStats.m_CharacterName;
            // Plugin.Logger.LogWarning("decision bug14 checking: " + name); // bug 14
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
            StringBuilder sb = new(DungeonEncounterRolls());
            if (sb.Length != 0)
            {
                sb.Append($"these roll chances are based on your {CombatUtils.entry.m_TestSkill} stat");
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
                    string btnName = btn.GetComponentInChildren<Text>().text;
                    // if btn text doesnt work
                    // if (GameDescriptions.AlternateLocLookUp.ContainsKey(btn.m_Option.ToString()))
                    // {
                    //     btnName = GameDescriptions.AlternateLocLookUp[btn.m_Option.ToString()];
                    // }
                    sb.AppendLine($"{CharacterData.GetCharacterName(cow)} [{btnName} ({GameDescriptions.VoteOptionDescriptions[btn.m_Option]})]");
                    sb.AppendLine($"{CombatUtils.GetDungeonSlotLegend(cow, btn.m_Option)}");
                    //expected => Cow [Disarm ()] 0(2%) = Failure
                }
            }
            if (sb.Length == 0) return "";
            sb.Insert(0, "(dungeon encounter rolls (actions with no roll results will always succeed) displayed as: character [action (description)] total successful rolls(chance for this result) = outcome result)\n");
            Plugin.Logger.LogWarning(sb.ToString());
            return sb.ToString();
        }
    }
}