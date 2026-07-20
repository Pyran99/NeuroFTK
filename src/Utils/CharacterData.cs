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

        public static SerializedCharacterData Calculate(CharacterOverworld cow) => new(cow);

        private SerializedCharacterData(CharacterOverworld cow)
        {
            Name = CharacterData.GetCharacterName(cow);
            Level = cow.m_CharacterStats.m_PlayerLevel;
            Xp = cow.m_CharacterStats.m_PlayerXP;
            Health = CharacterData.GetCharacterHealth(cow);
            Gold = cow.m_CharacterStats.m_Gold;
            FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(cow.m_CharacterStats.GetPipe());
            PipeItem = $"{FTKHub.Localized<TextItems>(pipe.m_DisplayName)} (lvl {int.Parse(pipe.m_ID.Replace("pipe", "")) - 1})";
        }
        
    }
}