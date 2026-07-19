using System;

namespace Pyran.NeuroFTK.Utils
{
    public class CharacterType
    {
        string Name { get; set; }
        string Description { get; set; }

        public static CharacterType SerializeGameClass()
        {
            throw new NotImplementedException();
        }

        private CharacterType()
        {
            Name = "";
            Description = "";
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