using System.Collections.Generic;
using System.Text;
using Google2u;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryStatusEffects() : NeuroAction
    {
        public override string Name => "query_status_effects";
        protected override string Description => "get a list of status effects, curses and immunities applied to the currently controlled character";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld cow = CharacterData.GetNeuroCow();
            StringBuilder sb = new($"(status effects on {CharacterData.GetCharacterName(cow)})\n[Effects]");
            string statusName;
            string statusDesc;
            List<ProficiencyBase> effects = CharacterData.GetStatusEffects(cow);
            bool added = false;
            foreach (ProficiencyBase prof in effects)
            {
                statusName = prof.m_ProficiencyData.GetLocalizedDisplayName();
                statusDesc = StatusEffects.GetCategoryDescription(prof);
                sb.Append($"{statusName} ({statusDesc}), ");
                added = true;
            }
            if (!added) sb.Append("none");
            sb.Append("\n[Curses] ");
            List<CharacterStats.CurseType> curses = CharacterData.GetCurses(cow);
            added = false;
            foreach (CharacterStats.CurseType curse in curses)
            {
                statusName = curse.ToString();
                statusDesc = FTKHub.Localized<TextInfo>("STR_status" + curse.ToString() + "Info");
                sb.Append($"{statusName} ({statusDesc}), ");
                added = true;
            }
            if (!added) sb.Append("none");
            sb.Append("\n[Immunities] ");
            List<ProficiencyBase.Category> immunities = CharacterData.GetImmunities(cow);
            added = false;
            foreach (ProficiencyBase.Category immunity in immunities)
            {
                statusName = immunity.ToString();
                if (GameDescriptions.AlternateLocLookUp.ContainsKey(statusName))
                {
                    statusName = GameDescriptions.AlternateLocLookUp[statusName];
                }
                sb.Append($"{statusName}, ");
                added = true;
            }
            if (cow.m_CharacterStats.IsDiseased)
            {
                sb.Append("\n[Disease] ");
                sb.Append($"{CharacterData.GetDiseaseData(cow)}");
            }
            if (!added) sb.Append("none");
            Context.Send(sb.ToString());
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}