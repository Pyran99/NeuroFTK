using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class ChooseRewardMenu
    {
        static ActionWindow window = null;
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
            if (buttons.Count == 1) // only cancel
            {
                uiChooseRewardButton first = buttons.First();
                Plugin.Logger.LogWarning("only 1 reward button " + first.m_Text.text);
                SelectButton.StartCoroutine(first, 1.0f);
                return;
            }
            if (dict.ContainsKey("Cancel")) dict.Remove("Cancel"); // assume always choose valid
            if (dict.Count == 0)
            {
                Plugin.Logger.LogError("no valid reward buttons");
                __instance.Close();
                return;
            }
            QuickTimerCallback timer = new(() => CreateAction(__instance, dict, title), __instance.m_DisplayRoot.gameObject);
        }

        [HarmonyPatch(typeof(uiChooseRewardMenu), nameof(uiChooseRewardMenu.Close))]
        [HarmonyPrefix]
        static void MenuClosed() // alt to unregister
        {
            buttons.Clear();
            Object.Destroy(window);
        }

        static void CreateAction(uiChooseRewardMenu _instance, Dictionary<string, uiChooseRewardButton> _buttons, string _title)
        {
            foreach (uiChooseRewardButton btn in buttons)
            {
                if (!btn.isActiveAndEnabled) return;
            }
            window = RewardMenuAction.RegisterActions(_instance, _buttons, _title);
            UnregisterDisabledObject.QuickCreate(_instance.gameObject, window, false);
        }
    }
}