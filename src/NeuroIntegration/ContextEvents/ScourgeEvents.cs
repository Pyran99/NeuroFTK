using System.Collections.Generic;
using System.Text;
using Google2u;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class ScourgeEvents
    {
        [HarmonyPatch(typeof(uiScourgeStatusEntry), nameof(uiScourgeStatusEntry.SetScourgeEntry))]
        [HarmonyPostfix]
        static void ScourgeUpdated(string _name, string _effect, MiniHexHaunt _haunt)
        {
            Plugin.Logger.LogWarning($"scourge set {_name}");
            Plugin.Logger.LogWarning($"scourge set {_effect}");
            SendScourgeContext();
        }

        [HarmonyPatch(typeof(uiScourgeStatusHUD), nameof(uiScourgeStatusHUD.AlertScourge))]
        [HarmonyPostfix]
        static void ScourgeAlert(MiniHexHaunt _mhh)
        {
            Plugin.Logger.LogWarning($"scourgeAlert1 {_mhh.GetHauntDBEntry().m_Scourge}");
            Plugin.Logger.LogWarning($"scourgeAlert2 {_mhh.GetIDString()}");
        }

        public static void SendScourgeContext()
        {
            uiScourgeStatusHUD hud = FTKUI.Instance.m_ScourgeStatusHud;
            List<uiScourgeStatusEntry> entries = [.. hud.transform.GetComponentsInChildren<uiScourgeStatusEntry>()];
            if (entries.Count == 0) return;
            StringBuilder sb = new();
            foreach (uiScourgeStatusEntry entry in entries)
            {
                if (!entry.gameObject.activeInHierarchy) continue;
                sb.AppendLine($"({entry.m_ScourgeName.text}) {FTKHub.Localized<TextInfo>(entry.m_ToolTip.m_Info)}");
            }
            if (sb.Length == 0) return;
            sb.Append("[active scourge events]", 0, 23);
            sb.Append($"(scourges can be cleared by defeating them)");
            Context.Send(sb.ToString());
        }
    }
}