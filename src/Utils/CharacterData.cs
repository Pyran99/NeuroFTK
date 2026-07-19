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
}