using System.Collections.Generic;
using System.Linq;
using Google2u;
using GridEditor;
using Pyran.NeuroFTK.GameConfigs;

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

        public static string GetCharacterClass(CharacterOverworld cow)
        {
            return cow.m_CharacterStats.m_CharacterClass.ToString();
        }

        public static CharacterOverworld GetNeuroCow(bool inCombat = false)
        {
            CharacterOverworld cow;
            if (Multiplayer.IsMultiplayer())
            {
                cow = Multiplayer.GetOwnCow();
            }
            else
            {
                cow = GameLogic.Instance.GetCurrentCOW();
                if (cow.m_CharacterStats.m_IsInCombat || cow.IsInDungeon())
                {
                    cow = GameLogic.Instance.GetCurrentCombatCOW();
                }
            }
            return cow;
        }

        public static List<ProficiencyBase> GetStatusEffects(CharacterOverworld cow)
        {
            List<ProficiencyBase> result = [];
            CharacterDummy dummy = cow.GetCurrentDummy();
            if (dummy == null) return result;
            foreach (CharacterDummy.ProficiencyRecord value in dummy.m_SufferingProficiencies.Values)
            {
                result.Add(value.m_Proficiency);
            }
            return result;
        }

        public static List<ProficiencyBase.Category> GetImmunities(CharacterOverworld cow)
        {
            CharacterStats stats = cow.m_CharacterStats;
            List<ProficiencyBase.Category> result = stats.GetAllActiveImmunities();
            return result;
        }

        public static List<CharacterStats.CurseType> GetCurses(CharacterOverworld cow)
        {
            List<CharacterStats.CurseType> result = [];
            CharacterStats stats = cow.m_CharacterStats;
            foreach (CharacterStats.CurseType curse in stats.m_ActiveCurses)
            {
                result.Add(curse);
            }
            return result;
        }

        public static IEnumerable<CharacterOverworld> GetCowsNotOnThisHex(CharacterOverworld currentCow)
        {
            IEnumerable<CharacterOverworld> allCows = FTKHub.Instance.m_CharacterOverworlds;
            HexLand curHex = currentCow.GetHexLand();
            return allCows.Where(cow => cow.GetHexLand() != curHex && HexLand.Distance(cow.GetHexLand(), curHex) < GlobalConfig.maxDistance);
        }
    }

    public sealed class SerializedCharacterData
    {
        public readonly string Name;
        public readonly string Class;
        public readonly int Level;
        public readonly int Xp;
        public readonly int Gold;
        public readonly string PipeItem;
        public readonly string Health;
        public readonly int Armor;
        public readonly int Resistance;
        public readonly List<string> StatusEffects = [];
        public readonly List<string> Immunities = [];
        public readonly List<string> Curses = [];

        public static SerializedCharacterData Calculate(CharacterOverworld cow) => new(cow);

        private SerializedCharacterData(CharacterOverworld cow)
        {
            Name = CharacterData.GetCharacterName(cow);
            Class = CharacterData.GetCharacterClass(cow);
            Level = cow.m_CharacterStats.m_PlayerLevel;
            Xp = cow.m_CharacterStats.m_PlayerXP;
            Health = CharacterData.GetCharacterHealth(cow);
            Gold = cow.m_CharacterStats.m_Gold;
            FTK_pipe.ID pipeID = cow.m_CharacterStats.GetPipe();
            FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(pipeID);
            PipeItem = $"{FTKHub.Localized<TextItems>(pipe.m_DisplayName)} (lvl {(int)pipeID})";
            StatusEffects = [.. CharacterData.GetStatusEffects(cow).Select(x => x.m_ProficiencyData.GetLocalizedDisplayName())];
            Immunities = [.. CharacterData.GetImmunities(cow).Select(x => x.ToString())];
            Curses = [.. CharacterData.GetCurses(cow).Select(x => x.ToString())];
            Armor = cow.m_CharacterStats.TotalArmor;
            Resistance = cow.m_CharacterStats.TotalResist;
        }
        
    }
}