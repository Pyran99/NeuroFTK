using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    [HarmonyPatch]
    public class PingHexData
    {
        [HarmonyPatch(typeof(HexLand), nameof(HexLand.Ping))]
        [HarmonyPostfix]
        static void Ping(HexLand __instance, bool _on, CharacterOverworld _cow)
        {
            if (!_on || !GlobalConfig.gameInitialized) return;
            Vector2 pos = HexData.GetVec2Pos(__instance);
            float distance;
            if (Multiplayer.IsMultiplayer())
            {
                distance = (float)Math.Round(HexLand.Distance(Multiplayer.GetOwnCow().m_HexLand, __instance), 1);
            }
            else
            {
                distance = (float)Math.Round(HexLand.Distance(CharacterData.GetNeuroCow().m_HexLand, __instance), 1);
            }
            Context.Send($"{CharacterData.GetCharacterName(_cow)} pinged {pos}. you are {distance} hexes away");
            if (!GlobalConfig.IsDebugMode()) return;
            StringBuilder sb = new();
            sb.AppendLine("ping data");
            sb.AppendLine($"id: {__instance.GetHexLandID().m_BigIndex} - {__instance.GetHexLandID().m_SmallIndex}");
            sb.AppendLine($"pos: {__instance.GetPosition()}");
            sb.AppendLine($"realm: {__instance.GetRealm()}"); // GuardianForest
            sb.AppendLine($"boat: {__instance.IsBoat()}");
            sb.AppendLine($"loc display: {__instance.GetLocationDisplayValue(CharacterData.GetNeuroCow())}"); // The Guardian Forest, is realm display if not dungeon
            sb.AppendLine($"distance: {distance}");
            // _ = HexLand.FindPath(CharacterData.GetNeuroCow().m_HexLand, __instance, HexLand.PathFindingStartState.OnLand, ref list);
            HexLand last = Movement.Instance.m_HexListPartial.Last();
            sb.AppendLine($"path end: {last?.GetPosition()}"); // is giving correct last valid move hex for hex's to far
            MiniHexInfo poi = __instance.GetPOI();
            if (poi)
            {
                sb.AppendLine($"poi skill: {poi?.GetPOIProfile().m_SkillRequired}"); // fortitude
                sb.AppendLine($"poi display: {poi?.GetPOIDisplayValue()}: {poi?.m_MiniHexType}"); // Cult Device
            }
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