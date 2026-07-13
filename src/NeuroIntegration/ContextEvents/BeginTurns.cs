using System.Collections.Generic;
using System.Text;
using GridEditor;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public class BeginTurns
    {
        public static void CtxOverworldTurnBeginStats(CharacterOverworld _cow)
        {
            if (ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            CharacterStats stats = _cow.m_CharacterStats;
            StringBuilder sb = new();
            sb.AppendLine($"[Begin turn overworld]");
            sb.AppendLine($"name: {stats.m_CharacterName} ({stats.m_CharacterClass})");
            sb.AppendLine($"lvl: {stats.m_PlayerLevel}");
            sb.AppendLine($"xp: {stats.GetXpDisplayString()}");// ({(float)Math.Round(stats.GetXpPercent()*100, 1)}%)");
            sb.AppendLine($"health: {stats.GetHealthDisplayString()}");// ({(float)Math.Round(stats.GetHealthPercent()*100, 1)}%)");
            sb.AppendLine($"gold: {stats.m_Gold}");
            FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(stats.GetPipe());
            sb.AppendLine($"pipe: {pipe.m_DisplayName}({(int)stats.GetPipe()}) (upgraded at the market)");
            Context.Send(sb.ToString());
        }

        public static void CtxCombatTurnBeginPlayer()
        {
            StringBuilder sb = new();
            foreach (KeyValuePair<FTKPlayerID, CharacterDummy> cow in EncounterSession.Instance.m_PlayerDummies)
            {
                if (!cow.Value.m_IsAlive) continue;
                CharacterStats stats = cow.Value.m_CharacterOverworld.m_CharacterStats;
                string name = $"{stats.m_CharacterName}";
                string health = $"{stats.GetHealthDisplayString()}";
                string coherent = cow.Value.IsCoherent() ? "" : "stunned";
                sb.AppendLine($"{name} (health: {health}) {coherent}");
            }
            Context.Send(sb.ToString());
        }

        public static void CtxCombatTurnBeginEnemy()
        {
            StringBuilder sb = new();
            // Dictionary<EnemyDummy, uiEachEnemyHud> enemies = new(uiEnemyHUD.Instance.m_EnemyHudDictionary);
            foreach (KeyValuePair<FTKPlayerID, EnemyDummy> enemy in EncounterSession.Instance.m_EnemyDummies)
            {
                if (!enemy.Value.m_IsAlive) continue;
                EnemyDummy _dummy = enemy.Value;
                FTK_enemyCombat _enemy = _dummy.GetEnemyInfo().m_EnemyCombat;
                string name = $"{_enemy.GetEnemyDisplay()}";
                string lvl = $"{_enemy.GetEnemyLevelDisplay()}";
                string health = $"{_dummy.GetEnemyInfo().GetCurrentHealth()}";
                string coherent = _dummy.IsCoherent() ? "" : "stunned";
                sb.AppendLine($"{name} (lvl {lvl}, health {health}) {coherent}");
            }
            Context.Send(sb.ToString());
        }
        
    }
}