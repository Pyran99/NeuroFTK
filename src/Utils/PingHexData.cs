using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using Pyran.NeuroFTK.GameConfigs;

namespace Pyran.NeuroFTK.Utils
{
    [HarmonyPatch]
    public class PingHexData
    {
        [HarmonyPatch(typeof(HexLand), nameof(HexLand.TogglePing))]
        [HarmonyPostfix]
        static void Ping(HexLand __instance)
        {
            if (GlobalConfig.debug_mode == false) return;
            Plugin.Logger.LogMessage("ping data");
            StringBuilder sb = new();
            sb.AppendLine($"\nid: {__instance.GetHexLandID().m_BigIndex} - {__instance.GetHexLandID().m_SmallIndex}");
            sb.AppendLine($"pos: {__instance.GetPosition()}");
            sb.AppendLine($"realm: {__instance.GetRealm()}"); // GuardianForest
            sb.AppendLine($"boat: {__instance.IsBoat()}");
            sb.AppendLine($"loc display: {__instance.GetLocationDisplayValue(GameLogic.Instance.GetCurrentCOW())}"); // The Guardian Forest, is realm display if not dungeon
            sb.AppendLine($"distance: {Math.Round(HexLand.Distance(GameLogic.Instance.GetCurrentCOW().m_HexLand, __instance), 2)}");
            // _ = HexLand.FindPath(GameLogic.Instance.GetCurrentCOW().m_HexLand, __instance, HexLand.PathFindingStartState.OnLand, ref list);
            HexLand last = Movement.Instance.m_HexListPartial.Last();
            sb.AppendLine($"path end: {last?.GetPosition()}"); // is giving correct last valid move hex for hex's to far
            MiniHexInfo poi = __instance.GetPOI();
            sb.AppendLine($"poi skill: {poi?.GetPOIProfile().m_SkillRequired}"); // fortitude
            sb.AppendLine($"poi display: {poi?.GetPOIDisplayValue()}"); // Cult Device
            if (TileHasQuestObjective(__instance, out QuestLogicBase quest))
            {
                sb.AppendLine($"quest desc: {StringReplace.RemoveStyling(quest.GetLocalizedOneLineDesc())}"); // Kill the <color=#FBB060>Chaos Leader</color> in <color=#FBB060>The Guardian Forest</color>
                QuestDefBase def = quest.GetQuestDef();
                if (def != null)
                {
                    sb.AppendLine($"def display: {def.m_DisplayName}"); // ""
                }
            }
            Plugin.Logger.LogMessage(sb.ToString());
        }

        static bool TileHasQuestObjective(HexLand hex, out QuestLogicBase quest)
        {
            MiniHexInfo poi = hex.GetPOI();
            quest = poi?.GetEncounterQuest();
            bool result = quest != null;
            if (!result)
            {
                if (poi?.GetFirstQuest() != null)
                {
                    quest = poi.GetFirstQuest();
                    result = true;
                }
            }
            return result;
        }
    }
}