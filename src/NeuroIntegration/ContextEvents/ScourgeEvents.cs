using System.Collections.Generic;
using System.Text;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
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
            if (!GlobalConfig.gameInitialized) return;
            Context.Send($"scourge effect activated: ({_name}) {FTKHub.Localized<TextInfo>(_effect)}");
        }

        [HarmonyPatch(typeof(MiniHexHaunt), nameof(MiniHexHaunt.DisableHaunt))]
        [HarmonyPrefix]
        static void ScourgeDisabled(MiniHexHaunt __instance)
        {
            Context.Send($"{FTKHub.Localized<TextEnemy>("STR_" + __instance.GetHauntDBEntry().m_Scourge)} scourge has been disabled");
        }

        [HarmonyPatch(typeof(uiScourgeStatusHUD), nameof(uiScourgeStatusHUD.AlertScourge))]
        [HarmonyPostfix]
        static void OnScourgeTriggered(MiniHexHaunt _mhh)
        {
            Plugin.Logger.LogWarning($"verify scourge alert => {FTKHub.Localized<TextInfo>("STR_" + _mhh.GetIDString() + "Effect")}"); // send context when scourge triggered
            Plugin.Logger.LogWarning($"verify scourge activate func => {_mhh.GetHauntDBEntry()?.m_ActivateFunction}");
        }

        public static string GetScourgeContext()
        {
            StringBuilder sb = new();
            MiniHexHaunt haunt;
            uiScourgeStatusEntry entry;
            foreach (KeyValuePair<FTK_enemyCombat.ID, MiniHexHaunt> scourge in GameLogic.Instance.m_HauntManager.m_HauntDictionary)
            {
                haunt = scourge.Value;
                if (!haunt.m_HauntActive) continue;
                entry = haunt.m_ThisScourgeEntry;
                sb.AppendLine($"- ({entry.m_ScourgeName.text} at {HexData.GetVec2Pos(haunt.m_HexLand)}) {FTKHub.Localized<TextInfo>(entry.m_ToolTip.m_Info)}");
            }
            if (sb.Length > 0) sb.Insert(0, $"### active scourge events \n");
            else return "";
            sb.Append($"(scourges can be cleared by defeating them)");
            return sb.ToString();
            // uiScourgeStatusHUD hud = FTKUI.Instance.m_ScourgeStatusHud;
            // List<uiScourgeStatusEntry> entries = [.. hud.transform.GetComponentsInChildren<uiScourgeStatusEntry>()];
            // if (entries.Count == 0) return;
            // foreach (uiScourgeStatusEntry entry in entries)
            // {
            //     if (!entry.gameObject.activeInHierarchy) continue;
            //     sb.AppendLine($"- ({entry.m_ScourgeName.text} ({entry.})) {FTKHub.Localized<TextInfo>(entry.m_ToolTip.m_Info)}");
            // }
            // if (sb.Length == 0) return;
        }
    }
}