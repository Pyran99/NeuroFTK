using System.Collections.Generic;
using System.Text;
using GridEditor;
using NeuroSdk.Internal;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public class BeginTurns
    {
        public static string CtxOverworldTurnBeginStats(CharacterOverworld _cow)
        {
            if (ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return "";
            SerializedCharacterData test = SerializedCharacterData.Calculate(_cow);
            string json = $"[{test.Name} turn] {Jason.Serialize(test)}";
            return json;
        }

        public static void CtxCombatTurnBeginPlayer()
        {
            StringBuilder sb = new();
            sb.Append("[team state]");
            foreach (KeyValuePair<FTKPlayerID, CharacterDummy> cow in EncounterSession.Instance.m_PlayerDummies)
            {
                if (!cow.Value.m_IsAlive) continue;
                CharacterStats stats = cow.Value.m_CharacterOverworld.m_CharacterStats;
                string name = $"{stats.m_CharacterName}";
                string lvl = $"{stats.m_PlayerLevel}";
                string health = $"{stats.GetHealthDisplayString()}";
                string coherent = cow.Value.IsCoherent() ? "" : "stunned";
                sb.AppendLine($"{name} (lvl {lvl}, health {health}) {coherent}.");
            }
            sb.AppendLine($"it is {CharacterData.GetCharacterName(GameLogic.Instance.GetCurrentCombatCOW())}'s turn.");
            Context.Send(sb.ToString());
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
                sb.AppendLine($"{name} (lvl {lvl}, health {health}) {coherent}.");
            }
            Context.Send(sb.ToString());
        }
        
    }
}