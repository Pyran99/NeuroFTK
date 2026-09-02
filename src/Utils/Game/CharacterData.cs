using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google2u;
using GridEditor;
using NeuroSdk.Json;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;
using UnityEngine;

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

        public static CharacterOverworld GetActiveCow()
        {
            CharacterOverworld cow;
            if (EncounterSessionMC.Instance.m_IsInCombat) cow = GameLogic.Instance.GetCurrentCombatCOW() ?? GameLogic.Instance.GetCurrentCOW();
            else cow = GameLogic.Instance.GetCurrentCOW();
            return cow;
        }

        public static string GetDataFor(CharacterOverworld cow)
        {
            StringBuilder sb = new();
            CharacterDummy dummy = cow.GetCurrentDummy();
            CharacterStats stats = cow.m_CharacterStats;
            string name = $"{stats.m_CharacterName}";
            if (stats.m_IsInCombat && !dummy.m_IsAlive) return $"{name} is dead.";
            string _class = $"{stats.m_CharacterClass}";
            string lvl = $"{stats.m_PlayerLevel}";
            string health = $"{stats.GetHealthDisplayString()}";
            string focus = $"{GetFocusAmount(cow)}";
            string coherent = "";
            if (stats.m_IsInCombat)
            {
                coherent = dummy.IsCoherent() ? "" : "stunned";
            }
            sb.Append($"({name}) {_class}, lvl {lvl}, health {health}, focus amount {focus}, {coherent}.");
            return sb.ToString();
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

        // only 1 disease at a time
        public static string GetDiseaseData(CharacterOverworld cow)
        {
            StringBuilder sb = new();
            CharacterStats stats = cow.m_CharacterStats;
            if (stats.IsDiseased)
            {
                DiseaseStatBase disease = stats.MyDisease;
                sb.AppendLine($"{GetDiseaseName(cow)}: ");
                sb.Append(disease.GetToolTipMoreInfo(stats));
            }
            return sb.ToString();
        }

        public static string GetDiseaseName(CharacterOverworld cow)
        {
            string result = "";
            if (cow.m_CharacterStats.IsDiseased)
            {
                result = $"({cow.m_CharacterStats.m_DiseaseLvl}){cow.m_CharacterStats.MyDisease.GetDiseaseTitle()}";
            }
            return result;
        }

        public static IEnumerable<CharacterOverworld> GetCowsNotOnThisHex(CharacterOverworld currentCow)
        {
            IEnumerable<CharacterOverworld> allCows = FTKHub.Instance.m_CharacterOverworlds;
            HexLand curHex = currentCow.GetHexLand();
            return allCows.Where(cow => cow.GetHexLand() != curHex && HexLand.Distance(cow.GetHexLand(), curHex) < GlobalConfig.maxDistance);
        }

        public static string GetAllStatusEffects(CharacterOverworld cow)
        {
            StringBuilder sb = new($"status effects on {GetCharacterName(cow)}:\n[Effects] ");
            string statusName;
            string statusDesc;
            List<ProficiencyBase> effects = GetStatusEffects(cow);
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
            List<CharacterStats.CurseType> curses = GetCurses(cow);
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
            List<ProficiencyBase.Category> immunities = GetImmunities(cow);
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
            if (!added) sb.Append("none");
            sb.Append("\n[Disease] ");
            added = false;
            if (cow.m_CharacterStats.IsDiseased)
            {
                sb.Append($"{GetDiseaseData(cow)}");
                added = true;
            }
            if (!added) sb.Append("none");
            return sb.ToString();
        }

        public static string GetTeamPositionState(CharacterOverworld _cow, HexLand hex, Dictionary<CharacterOverworld, HexLand> lastDestinations)
        {
            Vector2 pos = HexData.GetVec2Pos(hex);
            StringBuilder sb = new($"you are controlling {GetCharacterName(_cow)} at hex {pos}.");
            if (_cow.IsInBoat()) sb.Append(" you are in a boat.");
            else if (_cow.IsInAirShip()) sb.Append(" you are in an airship, you can leave it by moving onto an empty land hex then choosing the interact with hex action and 'Land' choice. you must leave the airship to interact with any point of interest on a hex.");
            if (lastDestinations.ContainsKey(_cow))
            {
                if (lastDestinations[_cow] != null && lastDestinations[_cow] != hex)
                {
                    pos = HexData.GetVec2Pos(lastDestinations[_cow]);
                    sb.Append($" the last hex you tried to move to with this character was {pos}.");
                }
            }
            foreach (CharacterOverworld player in FTKHub.Instance.m_CharacterOverworlds)
            {
                if (player == _cow) continue;
                string revive = player.m_WaitForRespawn ? " (waiting for revive)" : "";
                pos = HexData.GetVec2Pos(player.GetHexLand());
                sb.Append($" teammate {GetCharacterName(player)}{revive} is at hex {pos},");
            }
            return sb.ToString();
        }

        /// <returns>item id with each slot</returns>
        public static Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> GetAllEquipment(CharacterOverworld cow)
        {
            PlayerInventory.ContainerID[] containers =
            [
                PlayerInventory.ContainerID.LeftHand,
                PlayerInventory.ContainerID.RightHand,
                PlayerInventory.ContainerID.Head,
                PlayerInventory.ContainerID.Body,
                PlayerInventory.ContainerID.Foot,
                PlayerInventory.ContainerID.Neck,
                PlayerInventory.ContainerID.Trinket,
            ];
            PlayerInventory inv = cow.m_PlayerInventory;
            Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> items = [];
            foreach (PlayerInventory.ContainerID container in containers)
            {
                if (inv.Get(container).IsEmpty()) continue;
                items.Add(container, inv.Get(container).GetOne());
            }
            return items;
        }

        public static string GetFocusAmount(CharacterOverworld cow)
        {
            string result;
            int focus = cow.m_CharacterStats.m_FocusPoints;
            int maxFocus = cow.m_CharacterStats.MaxFocus;
            result = $"{focus}/{maxFocus}";
            return result;
        }

        public static JsonSchema QuickFocusSchema(CharacterOverworld cow)
        {
            int max = cow.m_CharacterStats.m_FocusPoints;
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Integer,
                Minimum = 0,
                Maximum = max
            };
            return schema;
        }

        public static bool CanFocusAction(CharacterStats stats, int slots, int usedPoints)
        {
            return usedPoints > 0 && stats.CanFocus() && stats.SpentFocus < slots;
        }

        public static string GetClassMainStat(FTK_playerGameStart.ID classId)
        {
            return classId switch
            {
                FTK_playerGameStart.ID.blacksmith => "strength/vitality",
                FTK_playerGameStart.ID.hunter => "awareness/speed",
                FTK_playerGameStart.ID.minstrel => "talent/intelligence",
                FTK_playerGameStart.ID.scholar => "intelligence/talent",
                FTK_playerGameStart.ID.busker => "talent/strength",
                FTK_playerGameStart.ID.herbalist => "intelligence/awareness",
                FTK_playerGameStart.ID.trapper => "awareness/talent",
                FTK_playerGameStart.ID.woodcutter => "Strength/vitality",
                FTK_playerGameStart.ID.hobo => "any",
                FTK_playerGameStart.ID.monk => "intelligence/strength",
                FTK_playerGameStart.ID.treasureHunter => "talent/awareness",
                FTK_playerGameStart.ID.astronomer => "intelligence/vitality",
                FTK_playerGameStart.ID.gladiator => "strength/awareness",
                FTK_playerGameStart.ID.professor => "NYI",
                _ => "",
            };
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
        public readonly string Focus;
        public readonly int Armor;
        public readonly int Resistance;
        public readonly List<string> StatusEffects = [];
        public readonly List<string> Immunities = [];
        public readonly List<string> Curses = [];
        public readonly string Disease;

        public static SerializedCharacterData Calculate(CharacterOverworld cow) => new(cow);

        private SerializedCharacterData(CharacterOverworld cow)
        {
            Name = CharacterData.GetCharacterName(cow);
            Class = CharacterData.GetCharacterClass(cow);
            Level = cow.m_CharacterStats.m_PlayerLevel;
            Xp = cow.m_CharacterStats.m_PlayerXP;
            Health = CharacterData.GetCharacterHealth(cow);
            Focus = CharacterData.GetFocusAmount(cow);
            Gold = cow.m_CharacterStats.m_Gold;
            FTK_pipe.ID pipeID = cow.m_CharacterStats.GetPipe();
            FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(pipeID);
            PipeItem = $"{FTKHub.Localized<TextItems>(pipe.m_DisplayName)} (lvl {(int)pipeID})";
            StatusEffects = [.. CharacterData.GetStatusEffects(cow).Select(x => x.m_ProficiencyData.GetLocalizedDisplayName())];
            Immunities = [.. CharacterData.GetImmunities(cow).Select(x => x.ToString())];
            Curses = [.. CharacterData.GetCurses(cow).Select(x => x.ToString())];
            Disease = CharacterData.GetDiseaseName(cow);
            Armor = cow.m_CharacterStats.TotalArmor;
            Resistance = cow.m_CharacterStats.TotalResist;
        }
        
    }
}