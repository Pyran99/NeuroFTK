using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class StatusEffects
    {
        static readonly Dictionary<string, List<GameObject>> immunities = [];
        static readonly Dictionary<string, List<GameObject>> ailments = [];
        static readonly bool ignore = true;

        // called often with any ui update. hover decision btns
        // [HarmonyPatch(typeof(uiPlayerMainHudStatus), nameof(uiPlayerMainHudStatus.SetStatusIcons))]
        // [HarmonyPostfix]
        static void OnSetIcons(uiPlayerMainHudStatus __instance)
        {
            if (ignore)
            {
                return;
            }
            // uiPlayerMainHud\DisplayRoot\playerMainHudStatus
            if (!GlobalConfig.gameInitialized) return;
            Plugin.Logger.LogWarning("testSetStatusIcons");
            uiPlayerMainHud hud = __instance.transform.parent.transform.parent.GetComponent<uiPlayerMainHud>();
            CharacterOverworld cow = hud.m_Cow;
            GameObject _immunities = __instance.transform.Find("immunities").gameObject;
            GameObject _ailments = __instance.transform.Find("aliments").gameObject;
            string name = CharacterData.GetCharacterName(cow);
            if (!immunities.ContainsKey(name)) immunities.Add(name, []);
            if (!ailments.ContainsKey(name)) ailments.Add(name, []);
            Dictionary<string, string> result = [];
            foreach (Transform child in _immunities.transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    if (immunities[name].Remove(child.gameObject))
                    {
                        Context.Send($"immunity removed {child.name}: {(effects2.TryGetValue(child.name, out result) ? $"{result.First().Key}, {result.First().Value}" : child.name)}");
                    }
                    continue;
                }
                Plugin.Logger.LogWarning($"immunity {child.name}: {(effects2.TryGetValue(child.name, out result) ? $"{result.First().Key}, {result.First().Value}" : child.name)}");
                if (immunities[name].Contains(child.gameObject)) continue;
                immunities[name].Add(child.gameObject);
                Context.Send($"immunity applied {child.name}: {(effects2.TryGetValue(child.name, out result) ? $"{result.First().Key}, {result.First().Value}" : child.name)}");
            }
            foreach (Transform child in _ailments.transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    if (ailments[name].Remove(child.gameObject))
                    {
                        Context.Send($"ailment removed {child.name}: {(effects2.TryGetValue(child.name, out result) ? $"{result.First().Key}, {result.First().Value}" : child.name)}");
                    }
                    continue;
                }
                Plugin.Logger.LogWarning($"ailment {child.name}: {(effects2.TryGetValue(child.name, out result) ? $"{result.First().Key}, {result.First().Value}" : child.name)}");
                if (ailments[name].Contains(child.gameObject)) continue;
                ailments[name].Add(child.gameObject);
                Context.Send($"ailment applied {child.name}: {(effects2.TryGetValue(child.name, out result) ? $"{result.First().Key}, {result.First().Value}" : child.name)}");
            }
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetNewCurseRPC))]
        [HarmonyPostfix]
        static void NewCurse()
        {
            Plugin.Logger.LogWarning("testNewCurse");
        }

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


        #region testing

        static string GetCategoryDescription(FTK_proficiencyTable table)
        {
			string text = string.Empty;
			if (table.m_ProficiencyPrefab)
			{
				ProficiencyBase proficiencyBase = ProficiencyManager.Instance.Get(FTK_proficiencyTable.GetEnum(table.m_ID));
				switch (table.m_ProficiencyPrefab.m_Category)
				{
				case ProficiencyBase.Category.Bleed:
					return FTKHub.Localized<TextMisc>("STR_profBleed");
				case ProficiencyBase.Category.Death:
					return FTKHub.Localized<TextMisc>("STR_profDeathmark");
				case ProficiencyBase.Category.Focus:
					return FTKHub.Localized<TextMisc>("STR_profFocus");
				case ProficiencyBase.Category.Fire:
					return FTKHub.Localized<TextMisc>("STR_profIgnite");
				case ProficiencyBase.Category.Ice:
					return FTKHub.Localized<TextMisc>("STR_profFreeze");
				case ProficiencyBase.Category.Lightning:
					return FTKHub.Localized<TextMisc>("STR_profShock");
				case ProficiencyBase.Category.Stunned:
					return FTKHub.Localized<TextMisc>("STR_profStun");
				case ProficiencyBase.Category.Time:
					if (proficiencyBase.m_CustomValue > 0)
					{
						text = FTKHub.Localized<TextMisc>("STR_HudSpeedUp");
					}
					else
					{
						text = FTKHub.Localized<TextMisc>("STR_HudSpeedDown");
					}
					return text;
				case ProficiencyBase.Category.Debuff:
					return FTKHub.Localized<TextMisc>("STR_profDebuff");
				case ProficiencyBase.Category.Attack:
					if (proficiencyBase.m_CustomValue > 0)
					{
						text = FTKHub.Localized<TextMisc>("STR_profAttackUp");
					}
					else
					{
						text = FTKHub.Localized<TextMisc>("STR_profAttackDown");
					}
					return text;
				case ProficiencyBase.Category.Armor:
					if (proficiencyBase.m_CustomValue > 0)
					{
						text = FTKHub.Localized<TextMisc>("STR_profArmorUp");
					}
					else
					{
						text = FTKHub.Localized<TextMisc>("STR_profArmorDown");
					}
					return text;
				case ProficiencyBase.Category.Scare:
					return FTKHub.Localized<TextMisc>("STR_profScare");
				case ProficiencyBase.Category.Reflect:
					return FTKHub.Localized<TextMisc>("STR_profReflect");
				case ProficiencyBase.Category.Interrupt:
					return FTKHub.Localized<TextMisc>("STR_profReset");
				case ProficiencyBase.Category.Rush:
					return FTKHub.Localized<TextMisc>("STR_profRush");
				case ProficiencyBase.Category.Cure:
					return FTKHub.Localized<TextMisc>("STR_profCure");
				case ProficiencyBase.Category.Resist:
					if (proficiencyBase.m_CustomValue > 0)
					{
						text = FTKHub.Localized<TextMisc>("STR_profResistUp");
					}
					else
					{
						text = FTKHub.Localized<TextMisc>("STR_profResistDown");
					}
					return text;
				case ProficiencyBase.Category.Evade:
					if (proficiencyBase.m_CustomValue > 0)
					{
						text = FTKHub.Localized<TextMisc>("STR_profEvadeUp");
					}
					else
					{
						text = FTKHub.Localized<TextMisc>("STR_profEvadeDown");
					}
					return text;
				case ProficiencyBase.Category.LifeDrain:
					return FTKHub.Localized<TextMisc>("STR_profLifeDrain");
				case ProficiencyBase.Category.Protect:
					if (table.m_Target == CharacterDummy.TargetType.Aoe)
					{
						text = FTKHub.Localized<TextMisc>("STR_profProtect2");
					}
					else
					{
						text = FTKHub.Localized<TextMisc>("STR_profProtect");
					}
					return text;
				case ProficiencyBase.Category.Dazed:
					return FTKHub.Localized<TextMisc>("STR_profDaze");
				case ProficiencyBase.Category.Water:
					return FTKHub.Localized<TextMisc>("STR_profWater");
				}
				text = "GetCategoryDescription #" + table.m_ProficiencyPrefab.m_Category.ToString() + "#";
			}
			return text;
        }




        [HarmonyPatch(typeof(CharacterDummy), nameof(CharacterDummy.AddProfToDummy))]
        [HarmonyPostfix]
        static void AddProfToDummy(FTK_proficiencyTable.ID[] _prof, CharacterDummy __instance)
        {
            Plugin.Logger.LogWarning("null 1");
            Plugin.Logger.LogWarning(_prof);
            Plugin.Logger.LogWarning("null 2");
            Plugin.Logger.LogWarning(__instance);
            Plugin.Logger.LogWarning("null 3");
            if (_prof == null || _prof.Length == 0) return;
            Plugin.Logger.LogWarning("null 4");
            StringBuilder sb = new();
            for (int i = 0; i < _prof.Length; i++)
            {
                Plugin.Logger.LogWarning("null 5");
                ProficiencyBase proficiencyBase = ProficiencyManager.Instance.Get(_prof[i]);
                if (!proficiencyBase) continue;
                if (proficiencyBase.IsImmune(__instance))
                {
                    Plugin.Logger.LogWarning($"immune to {_prof[i]}");
                    continue;
                }
                Plugin.Logger.LogWarning("null 6");
                FTK_proficiencyTable table = FTK_proficiencyTableDB.Get(_prof[i]);
                Plugin.Logger.LogWarning("null 7");
                sb.AppendLine($"added prof to {CharacterData.GetCharacterName(__instance.m_CharacterOverworld)}: {proficiencyBase.m_Category} (hud text {table?.GetLocalizedDisplayName()} (desc {GetCategoryDescription(table)}))");
                Plugin.Logger.LogWarning("null 8");
                // AddToDummy is called here on prof base
            }
            if (sb.Length == 0) return;
            Plugin.Logger.LogWarning(sb.ToString());
        }

        [HarmonyPatch(typeof(ProficiencyBase), nameof(ProficiencyBase.End))] // all overrides call base
        [HarmonyPostfix]
        static void ProfEnd(ProficiencyBase __instance, CharacterDummy _dummy)
        {
            FTK_proficiencyTable table = FTK_proficiencyTableDB.Get(__instance.m_ProficiencyID);
            Plugin.Logger.LogWarning($"base prof ended: {__instance.m_Category} (hud text {table?.GetLocalizedDisplayName()} (desc {GetCategoryDescription(table)}))");
        }

        [HarmonyPatch(typeof(CharacterDummy), nameof(CharacterDummy.RemoveSpecificProficiency))]
        [HarmonyPrefix]
        static void RemovedProficiency(ProficiencyBase.Category _c, CharacterDummy __instance)
        {
            if (__instance.m_SufferingProficiencies.ContainsKey(_c))
            {
                ProficiencyBase.Category _c2 = _c;
                Plugin.Logger.LogWarning("removed proficiency " + _c2.ToString());
            }
        }

        [HarmonyPatch(typeof(CharacterDummy), nameof(CharacterDummy.RemoveAllProficiencies))]
        [HarmonyPrefix]
        static void RemovedAllProficiency(CharacterDummy __instance)
        {
            StringBuilder sb = new();
            foreach (ProficiencyBase.Category category in __instance.m_SufferingProficiencies.Keys)
            {
                sb.AppendLine("removed proficiency " + category.ToString());
            }
            if (sb.Length == 0) return;
            Plugin.Logger.LogWarning(sb.ToString());
        }

        // [HarmonyPatch(typeof(ProficiencyBase), nameof(ProficiencyBase.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyBase __instance)
        // {
        //     Plugin.Logger.LogWarning($"added base prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyBleed), nameof(ProficiencyBleed.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyBleed __instance)
        // {
        //     Plugin.Logger.LogWarning($"added bleed prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyCure), nameof(ProficiencyCure.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyCure __instance)
        // {
        //     Plugin.Logger.LogWarning($"added cure prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyCurse), nameof(ProficiencyCurse.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyCurse __instance)
        // {
        //     Plugin.Logger.LogWarning($"added curse prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyDarkness), nameof(ProficiencyDarkness.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyDarkness __instance)
        // {
        //     Plugin.Logger.LogWarning($"added darkness prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyDeath), nameof(ProficiencyDeath.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyDeath __instance)
        // {
        //     Plugin.Logger.LogWarning($"added death prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyDebuff), nameof(ProficiencyDebuff.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyDebuff __instance)
        // {
        //     Plugin.Logger.LogWarning($"added debuff debuff prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyDisease), nameof(ProficiencyDisease.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyDisease __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyDrainLife), nameof(ProficiencyDrainLife.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyDrainLife __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyEntangle), nameof(ProficiencyEntangle.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyEntangle __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyFireBase), nameof(ProficiencyFireBase.AddToDummy))] // maybe remove
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyFireBase __instance)
        // {
        //     Plugin.Logger.LogWarning($"added fire prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyFocus), nameof(ProficiencyFocus.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyFocus __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyFullCure), nameof(ProficiencyFullCure.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyFullCure __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyIce), nameof(ProficiencyIce.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyIce __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyIlluminate), nameof(ProficiencyIlluminate.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyIlluminate __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyInterrupt), nameof(ProficiencyInterrupt.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyInterrupt __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyInvulnerability), nameof(ProficiencyInvulnerability.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyInvulnerability __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyLightning), nameof(ProficiencyLightning.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyLightning __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyPanaxCure), nameof(ProficiencyPanaxCure.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyPanaxCure __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyPetrify), nameof(ProficiencyPetrify.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyPetrify __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyPoison), nameof(ProficiencyPoison.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyPoison __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyQuickness), nameof(ProficiencyQuickness.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyQuickness __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyResistDeath), nameof(ProficiencyResistDeath.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyResistDeath __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyRush), nameof(ProficiencyRush.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyRush __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyScare), nameof(ProficiencyScare.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyScare __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyShield), nameof(ProficiencyShield.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyShield __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyStealBeltItem), nameof(ProficiencyStealBeltItem.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyStealBeltItem __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyStealEquippedItem), nameof(ProficiencyStealEquippedItem.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyStealEquippedItem __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyStealGold), nameof(ProficiencyStealGold.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyStealGold __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyStealPackItem), nameof(ProficiencyStealPackItem.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyStealPackItem __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyStunned), nameof(ProficiencyStunned.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyStunned __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyTaunt), nameof(ProficiencyTaunt.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyTaunt __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyVex), nameof(ProficiencyVex.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyVex __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }

        // [HarmonyPatch(typeof(ProficiencyWater), nameof(ProficiencyWater.AddToDummy))]
        // [HarmonyPostfix]
        // static void AddProficiency(CharacterDummy _dummy, ProficiencyWater __instance)
        // {
        //     Plugin.Logger.LogWarning($"added prof to {_dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName}: {__instance.m_Category}");
        // }


        #endregion

    }
}