using System;
using System.Text;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public class BeginTurns
    {
        public static void SendOverworldTurnBeginStats(CharacterOverworld _cow)
        {
            if (ToggleOverworldActions.mode != uiGameTrackerHUD.GameTrackerMode.Overworld) return;
            StringBuilder sb = new();
            sb.AppendLine($"[Begin turn overworld]");
            sb.AppendLine($"name: {_cow.m_CharacterStats.m_CharacterName} ({_cow.m_CharacterStats.m_CharacterClass})");
            sb.AppendLine($"character lvl: {_cow.m_CharacterStats.m_PlayerLevel}");
            sb.AppendLine($"xp: {_cow.m_CharacterStats.GetXpDisplayString()}");// ({(float)Math.Round(_cow.m_CharacterStats.GetXpPercent()*100, 1)}%)");
            sb.AppendLine($"health: {_cow.m_CharacterStats.GetHealthDisplayString()}");// ({(float)Math.Round(_cow.m_CharacterStats.GetHealthPercent()*100, 1)}%)");
            sb.AppendLine($"gold: {_cow.m_CharacterStats.m_Gold}");
            sb.AppendLine($"pipe lvl: {_cow.m_CharacterStats.m_Pipe.ToString().Replace("pipe", "")}");
            Context.Send(sb.ToString());
        }
        
    }
}