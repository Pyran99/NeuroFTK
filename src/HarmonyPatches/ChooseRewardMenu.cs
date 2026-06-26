using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
using Pyran.NeuroFTK.Utils;
using WebSocketSharp;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class ChooseRewardMenu
    {
        static List<uiChooseRewardButton> buttons = [];
        static string title = "";

        [HarmonyPatch(typeof(uiChooseRewardMenu), "BaseInitialize")]
        [HarmonyPostfix]
        static void Init(string _title, uiChooseRewardMenu.RewardType _rType)
        {
            buttons.Clear();
            title = _title;
        }

        [HarmonyPatch(typeof(uiChooseRewardMenu), "BaseInitialize2")]
        [HarmonyPostfix]
        static void Init2(uiChooseRewardMenu __instance, List<uiChooseRewardButton> ___m_AllButtons)
        {
            if (title.IsNullOrEmpty())
            {
                Plugin.Logger.LogError("reward title was empty");
                return;
            }
            buttons = [.. ___m_AllButtons];
            Plugin.Logger.LogMessage($"{string.Join(", ", [.. buttons.Select(x => x.m_Text.text)])}");
            Dictionary<string, uiChooseRewardButton> dict = buttons.ToDictionary(x => x.m_Text.text);
            if (dict.ContainsKey("Cancel")) dict.Remove("Cancel"); // assume always choose valid
            QuickTimerCallback timer = new(() => RewardMenuAction.RegisterActions(__instance, dict, title), 1000f);
        }
    }
}