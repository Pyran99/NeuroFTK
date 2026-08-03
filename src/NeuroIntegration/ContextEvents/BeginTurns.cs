using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using NeuroSdk.Internal;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public class BeginTurns
    {
        public static string CtxOverworldTurnBeginStats(CharacterOverworld _cow)
        {
            if (GameStates.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return "";
            SerializedCharacterData test = SerializedCharacterData.Calculate(_cow);
            string json = $"[{test.Name} turn] {Jason.Serialize(test)}";
            return json;
        }

        public static void CtxCombatTurnBeginPlayer(CharacterOverworld _cow)
        {
            StringBuilder sb = new();
            SerializedCharacterData data = SerializedCharacterData.Calculate(_cow);
            string json = $"[{data.Name} turn] {Jason.Serialize(data)}";
            sb.AppendLine(json);
            sb.Append("[teammates state]");
            foreach (KeyValuePair<FTKPlayerID, CharacterDummy> cow in EncounterSession.Instance.m_PlayerDummies)
            {
                if (!cow.Value.m_IsAlive) continue;
                if (cow.Value.m_CharacterOverworld == _cow) continue;
                CharacterStats stats = cow.Value.m_CharacterOverworld.m_CharacterStats;
                string name = $"{stats.m_CharacterName}";
                string _class = $"{stats.m_CharacterClass}";
                string lvl = $"{stats.m_PlayerLevel}";
                string health = $"{stats.GetHealthDisplayString()}";
                string coherent = cow.Value.IsCoherent() ? "" : "stunned";
                sb.AppendLine($"({name}) class {_class}, lvl {lvl}, health {health}, {coherent}.");
            }
            Context.Send(sb.ToString());
        }

        public static string GetSimplifiedTeamState()
        {
            StringBuilder sb = new();
            foreach (KeyValuePair<FTKPlayerID, CharacterDummy> cow in EncounterSession.Instance.m_PlayerDummies)
            {
                if (!cow.Value.m_IsAlive) continue;
                CharacterStats stats = cow.Value.m_CharacterOverworld.m_CharacterStats;
                string name = $"{stats.m_CharacterName}";
                string health = $"{stats.GetHealthDisplayString()}";
                string coherent = cow.Value.IsCoherent() ? "" : "stunned";
                sb.AppendLine($"({name}) health {health}, {coherent}");
            }
            return sb.ToString();
        }

        public static void CtxCombatTurnBeginEnemy()
        {
            StringBuilder sb = new();
            sb.Append("[enemy state]");
            // Dictionary<EnemyDummy, uiEachEnemyHud> enemies = new(uiEnemyHUD.Instance.m_EnemyHudDictionary);
            foreach (KeyValuePair<FTKPlayerID, EnemyDummy> enemy in EncounterSession.Instance.m_EnemyDummies)
            {
                if (!enemy.Value.m_IsAlive) continue;
                EnemyDummy _dummy = enemy.Value;
                FTK_enemyCombat _enemy = _dummy.GetEnemyInfo().m_EnemyCombat;
                string name = $"{CombatUtils.GetEnemyName(_dummy)}";
                string lvl = $"{_enemy.GetEnemyLevelDisplay()}";
                string health = $"{_dummy.GetEnemyInfo().GetCurrentHealth()}";
                string coherent = _dummy.IsCoherent() ? "" : "stunned";
                int armor = _dummy.GetArmor();
                int resist = _dummy.GetResist();
                List<string> immunities = EnemyImmunities(_dummy);
                string immunes = string.Join(", ", [.. immunities.Select(x => x)]);
                if (immunes.Length == 0) immunes = "none";
                sb.AppendLine($"{name}, lvl {lvl}, health {health}, armor {armor}, resist {resist}, {coherent} (immunities: {immunes})");
            }
            sb.Append($"(armor reduces physical dmg, resist reduces magic dmg)");
            Context.Send(sb.ToString());
        }

        static List<string> EnemyImmunities(EnemyDummy _enemy)
        {
            EnemyInfo enemy = _enemy.GetEnemyInfo();
            FTK_enemyCombat enemyCombat = enemy.m_EnemyCombat;
            List<string> result = [];
            if (enemyCombat.m_ImmuneBleed) result.Add("bleed");
            if (enemyCombat.m_ImmuneDistract) result.Add("distract");
            if (enemyCombat.m_ImmuneFire) result.Add("fire");
            if (enemyCombat.m_ImmuneIce) result.Add("freeze");
            if (enemyCombat.m_ImmuneLightning) result.Add("shock");
            if (enemyCombat.m_ImmuneStun) result.Add("stun/daze");
            if (enemyCombat.m_ImmuneWater) result.Add("wet");
            return result;
        }
        
    }
}