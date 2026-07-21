using System.Collections.Generic;
using Google2u;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public class CharacterData
    {
        public static string GetCharacterName(CharacterOverworld cow)
        {
            return cow.m_CharacterStats.m_CharacterName;
        }

        public static string GetCharacterHealth(CharacterOverworld cow)
        {
            return cow.m_CharacterStats.GetHealthDisplayString();
        }
    }

    public sealed class SerializedCharacterData
    {
        public readonly string Name;
        public readonly int Level;
        public readonly int Xp;
        public readonly string Health;
        public readonly int Gold;
        public readonly string PipeItem;
        public readonly List<string> StatusEffects = [];

        public static SerializedCharacterData Calculate(CharacterOverworld cow) => new(cow);

        private SerializedCharacterData(CharacterOverworld cow)
        {
            Name = CharacterData.GetCharacterName(cow);
            Level = cow.m_CharacterStats.m_PlayerLevel;
            Xp = cow.m_CharacterStats.m_PlayerXP;
            Health = CharacterData.GetCharacterHealth(cow);
            Gold = cow.m_CharacterStats.m_Gold;
            FTK_pipe.ID pipeID = cow.m_CharacterStats.GetPipe();
            FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(pipeID);
            PipeItem = $"{FTKHub.Localized<TextItems>(pipe.m_DisplayName)} (lvl {(int)pipeID})";
            StatusEffects = GetStatusEffects(cow);
        }

        private List<string> GetStatusEffects(CharacterOverworld cow)
        {
            List<string> result = [];
            CharacterDummy dummy = cow.GetCurrentDummy();
            if (dummy == null) return result;
            foreach (CharacterDummy.ProficiencyRecord value in dummy.m_SufferingProficiencies.Values)
            {
                result.Add(value.m_Proficiency.m_ProficiencyData.GetLocalizedDisplayName());
            }
            return result;
        }
        
    }
}