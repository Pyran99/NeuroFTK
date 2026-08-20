using System.Collections.Generic;
using System.Text;
using Google2u;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class ScourgeEvents
    {
        [HarmonyPatch(typeof(uiScourgeStatusEntry), nameof(uiScourgeStatusEntry.SetScourgeEntry))]
        [HarmonyPostfix]
        static void ScourgeUpdated(string _name, string _effect, MiniHexHaunt _haunt)
        {
            Plugin.Logger.LogMessage($"scourge set {_name}: {_effect}");
            SendScourgeContext();
        }

        [HarmonyPatch(typeof(MiniHexHaunt), nameof(MiniHexHaunt.DisableHaunt))]
        [HarmonyPrefix]
        static void ScourgeDisabled(MiniHexHaunt __instance)
        {
            Context.Send($"{FTKHub.Localized<TextEnemy>("STR_" + __instance.GetHauntDBEntry().m_Scourge)} scourge has been disabled");
        }

        public static void SendScourgeContext()
        {
            uiScourgeStatusHUD hud = FTKUI.Instance.m_ScourgeStatusHud;
            List<uiScourgeStatusEntry> entries = [.. hud.transform.GetComponentsInChildren<uiScourgeStatusEntry>()];
            if (entries.Count == 0) return;
            StringBuilder sb = new("[active scourge events]");
            foreach (uiScourgeStatusEntry entry in entries)
            {
                if (!entry.gameObject.activeInHierarchy) continue;
                sb.AppendLine($"({entry.m_ScourgeName.text}) {FTKHub.Localized<TextInfo>(entry.m_ToolTip.m_Info)}");
            }
            if (sb.Length == 23) return;
            sb.Append($"(scourges can be cleared by defeating them)");
            Context.Send(sb.ToString());
        }
    }
}