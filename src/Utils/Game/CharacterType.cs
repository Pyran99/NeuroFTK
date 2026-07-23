using Google2u;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public class CharacterType
    {
        public readonly string Class;
        public readonly string Description;
        public readonly string Weapon;
        public readonly string Items;
        public readonly string Abilities;
        public readonly int Gold;
        public readonly int Toughness;
        public readonly int Fortitude;
        public readonly int Talent;
        public readonly int Awareness;
        public readonly int Quickness;
        public readonly int Vitality;

        public static CharacterType SerializeGameClass(FTK_playerGameStart entry)
        {
            return new CharacterType(entry);
        }

        private CharacterType(FTK_playerGameStart entry)
        {
            Class = entry.GetDisplayName();
            Description = StringReplace.RemoveStyling(FTKHub.Localized<TextCharacters>(entry.m_Flavor));
            Abilities = StringReplace.ReplaceNewLine(entry.m_CharacterSkills.GetSkillDisplay(false));
            Weapon = entry.m_StartWeapon != FTK_itembase.ID.None ? FTKHub.Instance.GetItemDisplayName(entry.m_StartWeapon) : "";
            foreach (FTK_itembase.ID _id in entry.m_StartItems)
            {
                Items += $"{FTKHub.Instance.GetItemDisplayName(_id)}, ";
            }
            Gold = entry._startinggold + GameFlow.Instance.GameDif.m_ExtraGold;
            float statBonus = GameFlow.Instance.GameDif.m_StatBonus;
            Toughness = FTKUtil.RoundToInt((entry._toughness + statBonus) * 100f);
            Fortitude = FTKUtil.RoundToInt((entry._fortitude + statBonus) * 100f);
            Talent = FTKUtil.RoundToInt((entry._talent + statBonus) * 100f);
            Awareness = FTKUtil.RoundToInt((entry._awareness + statBonus) * 100f);
            Quickness = FTKUtil.RoundToInt((entry._quickness + statBonus) * 100f);
            Vitality = FTKUtil.RoundToInt((entry._vitality + statBonus) * 100f);
        }
    }
}

// public sealed class SerializeTest
// {
//     public readonly string Name;
//     public readonly int Level;
//     public readonly int Xp;
//     public readonly string Health;
//     public readonly int Gold;
//     public readonly int PipeItemLevel;

//     public static SerializeTest Calculate(CharacterOverworld cow) => new(cow);

//     private SerializeTest(CharacterOverworld cow)
//     {
//         Plugin.Logger.LogWarning("Create serialize test");
//         Name = CharacterData.GetCharacterName(cow);
//         Level = cow.m_CharacterStats.m_PlayerLevel;
//         Xp = cow.m_CharacterStats.m_PlayerXP;
//         Health = CharacterData.GetCharacterHealth(cow);
//         Gold = cow.m_CharacterStats.m_Gold;
//         FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(cow.m_CharacterStats.GetPipe());
//         PipeItemLevel = (int)pipe.m_PipeItem;
//     }