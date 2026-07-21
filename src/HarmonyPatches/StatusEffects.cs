using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class StatusEffects
    {

        readonly Dictionary<string, List<GameObject>> immunities = [];
        readonly Dictionary<string, List<GameObject>> ailments = [];
        
        [HarmonyPatch(typeof(uiPlayerMainHudStatus), nameof(uiPlayerMainHudStatus.SetStatusIcons))] //1. each on game start | 2. turn start, overworld: before tracking
        [HarmonyPostfix]
        static void OnSetIcons(uiPlayerMainHudStatus __instance)
        {
            // uiPlayerMainHud\DisplayRoot\playerMainHudStatus
            Plugin.Logger.LogWarning("testSetStatusIcons");
            uiPlayerMainHud hud = __instance.transform.parent.transform.parent.GetComponent<uiPlayerMainHud>();
            CharacterOverworld cow;
            GameObject immunities = __instance.transform.Find("immunities").gameObject;
            GameObject ailments = __instance.transform.Find("aliments").gameObject;
            foreach (Transform child in immunities.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                Plugin.Logger.LogWarning($"immunity {child.name}: {(effects2.TryGetValue(child.name, out Dictionary<string, string> _effect) ? $"{_effect.First().Key}, {_effect.First().Value}" : child.name)}");
            }
            foreach (Transform child in ailments.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                Plugin.Logger.LogWarning($"ailment {child.name}: {(effects2.TryGetValue(child.name, out Dictionary<string, string> _effect) ? $"{_effect.First().Key}, {_effect.First().Value}" : child.name)}");
            }
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetNewCurseRPC))]
        [HarmonyPostfix]
        static void NewCurse()
        {
            Plugin.Logger.LogWarning("testNewCurse");
        }

        static void Test(CharacterOverworld cow)
        {
            foreach (CharacterStats.CurseType curse in cow.m_CharacterStats.m_ActiveCurses)
            {
                
            }
        }

        readonly Dictionary<CharacterStats.CurseType, string> curses = new()
        {
            {CharacterStats.CurseType.Blind, "Blind"},
        };

        readonly Dictionary<ProficiencyBase.Category, string> proficiencies = new()
        {
            {ProficiencyBase.Category.Acid, "Blind"},
        };


        static readonly Dictionary<string, Dictionary<string, string>> effects2 = new()
        {
            {"ambushImmunity", new(){{"Ambush Immunity", "immune to ambush while moving"}}},
            {"bleedImmunity", new(){{"Bleed Immunity", "immune to bleed"}}},
            {"confuseImmunity", new(){{"Confuse Immunity", "immune to confuse"}}},
            {"curseImmunity", new(){{"Curse Immunity", "immune to curses"}}},
            {"deathmarkImmunity", new(){{"Deathmark Immunity", "immune to deathmark"}}},
            {"fireImmunity", new(){{"Fire Immunity", "immune to fire"}}},
            {"freezeImmunity", new(){{"Freeze Immunity", "immune to freeze"}}},
            {"poisonImmunity", new(){{"Poison Immunity", "immune to poison"}}},
            {"scareImmunity", new(){{"Scare Immunity", "immune to being scared"}}},
            {"shockImmunity", new(){{"Shock Immunity", "immune to being shocked"}}},
            {"stealImmunity", new(){{"Steal Immunity", "immune to being stolen from"}}},
            {"stunImmunity", new(){{"Stun Immunity", "immune to stun"}}},
            {"acidImmunity", new(){{"Acid Immunity", "immune to acid. FeelsHighMan"}}},
            {"wetImmunity", new(){{"Wet Immunity", "immune to being wet. very useful for a computer"}}},
            {"diseaseImmunity", new(){{"Disease Immunity", "immune to diseases"}}},
            {"petrifyImmunity", new(){{"Petrify Immunity", "immune to petrify"}}},

            {"lightFooted", new(){{"Light Footed", "light foot NYI"}}},
            {"resistDeath", new(){{"Resist Death", "resist death NYI"}}},
            {"curseUnlucky", new(){{"Curse Unlucky", "reduced luck stat"}}},
            {"curseFeeble", new(){{"Curse Feeble", "reduced strength stat"}}},
            {"curseBlind", new(){{"Curse Blind", "reduced awareness stat"}}},
            {"curseLethargic", new(){{"Curse Lethargic", "reduced speed stat"}}},
            {"curseFoolish", new(){{"Curse Foolish", "reduced intelligence stat"}}},
            {"curseUnwell", new(){{"Curse Unwell", "reduced vitality stat"}}},

            {"poisonIconLvl1", new(){{"Poison 1", "reduce all stats and deal low damage at the end of each of this characters turn, stacks"}}},
            {"poisonIconLvl2", new(){{"Poison 2", "reduce all stats and deal low damage at the end of each of this characters turn, stacks"}}},
            {"poisonIconLvl3", new(){{"Poison 3", "reduce all stats and deal low damage at the end of each of this characters turn, stacks"}}},
            {"confused", new(){{"Confused", "this character takes random actions without your input"}}},
            {"burning", new(){{"Burning", "high chance to deal low damage at the end of each of this characters turns"}}},
            {"shocked", new(){{"Shocked", "first roll will miss"}}},
            {"stunned", new(){{"Stunned", "delays next action"}}},
            {"frozen", new(){{"Frozen", "takes 25% more damage"}}},
            {"deathMark", new(){{"Death Mark", "death after several turns. this is removed at the end of battle"}}},
            {"bleeding", new(){{"Bleeding", "low chance to deal high damage at the end of each of this characters turns"}}},
            {"acid", new(){{"Acid", "destroys a random piece of equipment at the start of each of this characters turns"}}},
            {"entangled", new(){{"Entangled", "stopped from skipping ahead in the attack order"}}},
            {"wet", new(){{"Wet", "lose other immunities"}}},

            {"attackUp", new(){{"Attack up", "increase attack damage"}}},
            {"attackDown", new(){{"Attack down", "decrease attack damage"}}},
            {"resistUp", new(){{"Resist up", "increase resistance stat"}}},
            {"resistDown", new(){{"Resist down", "decrease resistance stat"}}},
            {"armorUp", new(){{"Armor up", "increase armor"}}},
            {"armorDown", new(){{"Armor down", "decrease armor"}}},
            {"evadeUp", new(){{"Evade up", "increase evade chance"}}},
            {"evadeDown", new(){{"Evade down", "decrease evade chance"}}},
            {"speedUp", new(){{"Speed up", "increase speed"}}},
            {"speedDown", new(){{"Speed down", "decrease speed"}}},
            {"resilient", new(){{"Resilient", "cannot be killed"}}},

            {"taunt", new(){{"Taunt", "attackers will focus on this character"}}},
            {"damageReflect", new(){{"Damage reflect", "reflect some damage back to melee attackers"}}},
            {"protect", new(){{"Protect", "completely negates the next attack"}}},
            {"reflect", new(){{"Reflect", "reflect damage back to attacker"}}},
            {"petrified", new(){{"Petrified", "cannot act until attacked or the effect wears off"}}},

            {"[1] groupShield", new(){{"Group shield", "negates the next 3 attacks"}}}, // dlc
            {"[1] disease", new(){{"", ""}}}, // dlc

            {"scared", new(){{"Scared", "flees on next turn"}}}, // TODO confirm object
            {"evasive", new(){{"Evasive", "evades any attack that did not roll perfect"}}}, // TODO confirm object
        };

        // game objects
        static readonly Dictionary<string, string> effects = new()
        {
            {"ambushImmunity", "Ambush Immunity"},
            {"bleedImmunity", "Bleed Immunity"},
            {"confuseImmunity", "Confuse Immunity"},
            {"curseImmunity", "Curse Immunity"},
            {"deathmarkImmunity", "Deathmark Immunity"},
            {"fireImmunity", "Fire Immunity"},
            {"freezeImmunity", "Freeze Immunity"},
            {"poisonImmunity", "Poison Immunity"},
            {"scareImmunity", "Scare Immunity"},
            {"shockImmunity", "Shock Immunity"},
            {"stealImmunity", "Steal Immunity"},
            {"stunImmunity", "Stun Immunity"},
            {"acidImmunity", "Acid Immunity"},
            {"wetImmunity", "Wet Immunity"},
            {"diseaseImmunity", "Disease Immunity"},
            {"petrifyImmunity", "Petrify Immunity"},
            {"lightFooted", "Light Footed"},
            {"resistDeath", "Resist Death"},
            {"curseUnlucky", "Curse Unlucky"},
            {"curseFeeble", "Curse Feeble"},
            {"curseBlind", "Curse Blind"},
            {"curseLethargic", "Curse Lethargic"},
            {"curseFoolish", "Curse Lethargic"},
            {"curseUnwell", "Curse Lethargic"},
            {"poisonIconLvl1", "Curse Lethargic"},
            {"poisonIconLvl2", "Curse Lethargic"},
            {"poisonIconLvl3", "Curse Lethargic"},
            {"confused", "Curse Lethargic"},
            {"burning", "Curse Lethargic"},
            {"shocked", "Curse Lethargic"},
            {"stunned", "Curse Lethargic"},
            {"frozen", "Curse Lethargic"},
            {"deathMark", "Curse Lethargic"},
            {"bleeding", "Curse Lethargic"},
            {"attackUp", "Curse Lethargic"},
            {"attackDown", "Curse Lethargic"},
            {"resistUp", "Curse Lethargic"},
            {"resistDown", "Curse Lethargic"},
            {"armorUp", "Curse Lethargic"},
            {"armorDown", "Curse Lethargic"},
            {"evadeUp", "Curse Lethargic"},
            {"evadeDown", "Curse Lethargic"},
            {"speedUp", "Curse Lethargic"},
            {"speedDown", "Curse Lethargic"},
            {"taunt", "Curse Lethargic"},
            {"acid", "Curse Lethargic"},
            {"entangled", "Curse Lethargic"},
            {"damageReflect", "Curse Lethargic"},
            {"protect", "Curse Lethargic"},
            {"[1] groupShield", "Curse Lethargic"},
            {"reflect", "Curse Lethargic"},
            {"wet", "Curse Lethargic"},
            {"[1] disease", "Curse Lethargic"},
            {"petrified", "Curse Lethargic"},
        };
    }
}