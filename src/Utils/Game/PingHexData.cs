using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;

namespace Pyran.NeuroFTK.Utils
{
    [HarmonyPatch]
    public class PingHexData
    {
        public static List<HexLand> activePings = [];

        [HarmonyPatch(typeof(HexLand), nameof(HexLand.Ping))]
        [HarmonyPostfix]
        static void Ping(HexLand __instance, bool _on, CharacterOverworld _cow)
        {
            if (!GlobalConfig.gameInitialized)
            {
                if (_on && !activePings.Contains(__instance)) activePings.Add(__instance);
                return;
            }
            if (!GlobalConfig.IsDebugMode()) return;
            float distance;
            StringBuilder sb2 = new();
            if (Multiplayer.IsMultiplayer())
            {
                foreach (CharacterOverworld cow in Multiplayer.GetAllOwnCows())
                {
                    distance = (float)Math.Round(HexLand.Distance(cow.m_HexLand, __instance), 1);
                    sb2.Append($"{CharacterData.GetCharacterName(cow)} is {distance} hexes away, ");
                }
            }
            else
            {
                CharacterOverworld active = CharacterData.GetActiveCow();
                distance = (float)Math.Round(HexLand.Distance(active.m_HexLand, __instance), 1);
                sb2.Append($"{CharacterData.GetCharacterName(active)} is {distance} hexes away");
            }
            StringBuilder sb = new();
            sb.AppendLine("ping data");
            sb.AppendLine($"id: {__instance.GetHexLandID().m_BigIndex} - {__instance.GetHexLandID().m_SmallIndex}");
            sb.AppendLine($"pos: {__instance.GetPosition()}");
            sb.AppendLine($"realm: {__instance.GetRealm()}"); // GuardianForest
            sb.AppendLine($"boat: {__instance.IsBoat()}");
            sb.AppendLine($"loc display: {__instance.GetLocationDisplayValue(CharacterData.GetActiveCow())}"); // The Guardian Forest, is realm display if not dungeon
            sb.AppendLine($"distance: {sb2}");
            // _ = HexLand.FindPath(CharacterData.GetActiveCow().m_HexLand, __instance, HexLand.PathFindingStartState.OnLand, ref list);
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

        [HarmonyPatch(typeof(HexLand), nameof(HexLand.TogglePing))]
        [HarmonyPrefix]
        static void TogglePing(HexLand __instance, CharacterOverworld _cow)
        {
            if (!GlobalConfig.gameInitialized) return;
            string name = CharacterData.GetCharacterName(_cow);
            if (_cow.m_PingHex != HexLandID.NullHexLandID)
            {
                activePings.Remove(_cow.m_PingHex.GetHexLand());
                if (_cow.m_PingHex == __instance.GetHexLandID())
                {
                    Context.Send($"{name} remove ping from {HexData.GetVec2String(_cow.m_PingHex.GetHexLand())}", true);
                    return;
                }
            }

            if (!activePings.Contains(__instance)) activePings.Add(__instance);
            float distance;
            StringBuilder sb = new();
            if (Multiplayer.IsMultiplayer())
            {
                foreach (CharacterOverworld cow in Multiplayer.GetAllOwnCows())
                {
                    distance = (float)Math.Round(HexLand.Distance(cow.m_HexLand, __instance), 1);
                    sb.Append($"{CharacterData.GetCharacterName(cow)} is {distance} hexes away, ");
                }
            }
            else
            {
                CharacterOverworld active = CharacterData.GetActiveCow();
                distance = (float)Math.Round(HexLand.Distance(active.m_HexLand, __instance), 1);
                sb.Append($"{CharacterData.GetCharacterName(active)} is {distance} hexes away");
            }
            Context.Send($"{name} pinged {HexData.GetVec2String(__instance)}. {sb}");
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