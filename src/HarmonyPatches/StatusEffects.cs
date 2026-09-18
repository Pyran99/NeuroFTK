using System.Collections;
using System.Linq;
using System.Text;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class StatusEffects
    {
        static StringBuilder statusCtx = new();
        static bool statusWaiting = false;
        static StringBuilder statusEndCtx = new();
        static bool statusEndWaiting = false;

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetNewCurseRPC))]
        [HarmonyPostfix]
        static void NewCurse(CharacterStats.CurseType _type, CharacterStats __instance)
        {
            if (_type == CharacterStats.CurseType.None || _type == CharacterStats.CurseType.COUNT) return;
            if (__instance.HasImmunity(ProficiencyBase.Category.Curse)) return;
            statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format([_type, FTKHub.Localized<TextInfo>(GetCurseDescription(_type)), CharacterData.GetCharacterName(__instance.m_CharacterOverworld)]));
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.RemoveAllActiveCurses))]
        [HarmonyPostfix]
        static void RemovedAllCurses(CharacterStats __instance)
        {
            Context.Send($"removed all curses from {CharacterData.GetCharacterName(__instance.m_CharacterOverworld)}");
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.UpdateDisease))]
        [HarmonyPostfix]
        static void UpdateDisease(string _diseaseType, int _plusLevel, CharacterStats __instance)
        {
            if (_diseaseType == string.Empty) return;
            if (__instance.HasImmunity(ProficiencyBase.Category.Disease)) return;
            statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format([_diseaseType, FTKHub.Localized<TextInfo>("STR_statusDiseasedInfo"), CharacterData.GetCharacterName(__instance.m_CharacterOverworld)]));
        }

        [HarmonyPatch(typeof(CharacterDummy), nameof(CharacterDummy.AddProfToDummy))]
        [HarmonyPostfix]
        static void AddProfToDummy(FTK_proficiencyTable.ID[] _prof, CharacterDummy __instance)
        {
            if (_prof == null || _prof.Length == 0) return;
            for (int i = 0; i < _prof.Length; i++)
            {
                ProficiencyBase proficiencyBase = ProficiencyManager.Instance.Get(_prof[i]);
                if (!proficiencyBase) continue;
                if (proficiencyBase.m_Category == ProficiencyBase.Category.Curse) continue; // chosen at random after this method
                if (proficiencyBase.m_Category == ProficiencyBase.Category.Disease) continue;
                if (proficiencyBase.m_Category == ProficiencyBase.Category.Poison) continue;
                if (proficiencyBase.IsImmune(__instance))
                {
                    Plugin.Logger.LogWarning($"immune to {_prof[i]}");
                    continue;
                }
                StatusAppliedCtx(proficiencyBase, __instance);
            }
            if (statusCtx.Length == 0) return;
            if (statusWaiting) return;
            statusWaiting = true;
            __instance.StartCoroutine(StatusAppliedWait());
        }

        [HarmonyPatch(typeof(ProficiencyBase), nameof(ProficiencyBase.End))] // dummy RemoveProf calls, all overrides call base
        [HarmonyPrefix]
        static void ProfEnd(ProficiencyBase __instance, CharacterDummy _dummy)
        {
            // if (!_dummy.m_SufferingProficiencies.ContainsKey(__instance.m_Category)) return;
            StatusRemoveCtx(__instance, _dummy);
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.UpdatePoison))]
        [HarmonyPostfix]
        static void PoisonUpdated(CharacterStats __instance, int _poison)
        {
            if (_poison > 0)
            {
                statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format(["poison", FTKHub.Localized<TextInfo>("STR_statusPoisonInfo"), CharacterData.GetCharacterName(__instance.m_CharacterOverworld)]));
            }
            else
            {
                statusCtx.AppendLine(StringMessages.StatusEffectRemoved.Format(["poison", FTKHub.Localized<TextInfo>("STR_statusPoisonInfo"), CharacterData.GetCharacterName(__instance.m_CharacterOverworld)]));
            }
            if (statusWaiting) return;
            statusWaiting = true;
            __instance.StartCoroutine(StatusAppliedWait());
        }

        static void StatusAppliedCtx(ProficiencyBase prof, CharacterDummy _dummy)
        {
            if (prof is ProficiencyStealBase) return;
            string statusName = prof.m_ProficiencyData.GetLocalizedDisplayName(); // used for spawned text
            string desc = GetCategoryDescription(prof);
            // Burning (Take frequent light damage) applied to Goblin Assassin
            if (_dummy.m_CharacterOverworld == null)
            {
                // enemy dummy doesnt have overworld
                statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format([statusName, desc, CombatUtils.GetEnemyName(_dummy as EnemyDummy)]));
            }
            else
            {
                statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format([statusName, desc, CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)]));
            }
            // AddToDummy is called here on prof base
        }

        static IEnumerator StatusAppliedWait()
        {
            yield return null;
            statusWaiting = false;
            Context.Send(statusCtx.ToString());
            statusCtx = new();
        }

        static void StatusRemoveCtx(ProficiencyBase prof, CharacterDummy _dummy)
        {
            string statusName = prof.m_ProficiencyData.GetLocalizedDisplayName();
            string desc = GetCategoryDescription(prof);
            if (_dummy.m_CharacterOverworld == null)
            {
                statusEndCtx.AppendLine(StringMessages.StatusEffectRemoved.Format([statusName, desc, CombatUtils.GetEnemyName(_dummy as EnemyDummy)]));
            }
            else
            {
                statusEndCtx.AppendLine(StringMessages.StatusEffectRemoved.Format([statusName, desc, CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)]));
            }
            if (statusEndWaiting) return;
            statusEndWaiting = true;
            _dummy.StartCoroutine(StatusRemovedWait());
        }

        static IEnumerator StatusRemovedWait()
        {
            yield return null;
            statusEndWaiting = false;
            Context.Send(statusEndCtx.ToString());
            statusEndCtx = new();
        }

        public static string GetCategoryDescription(ProficiencyBase prof)
        {
			string result = string.Empty;
			if (!prof) return "";
            switch (prof.m_Category)
            {
                case ProficiencyBase.Category.Acid:
                    result = "STR_statusAcidInfo";
                    break;
                case ProficiencyBase.Category.Armor:
                    if (prof.m_CustomValue > 0) result = "STR_statusArmorUpInfo";
                    else result = "STR_statusArmorDownInfo";
                    break;
                case ProficiencyBase.Category.Attack:
                    if (prof.m_CustomValue > 0) result = "STR_statusAttackUpInfo";
                    else result = "STR_statusAttackDownInfo";
                    break;
                case ProficiencyBase.Category.Bleed:
                    result = "STR_statusBleedingInfo";
                    break;
                case ProficiencyBase.Category.Confuse:
                    result = "STR_statusConfusedInfo";
                    break;
                case ProficiencyBase.Category.Dazed:
                    result = "STR_statusDazedInfo";
                    break;
                case ProficiencyBase.Category.Death:
                    result = "STR_statusDeathMarkedInfo";
                    break;
                case ProficiencyBase.Category.Entangle:
                    result = "STR_statusEntangledInfo";
                    break;
                case ProficiencyBase.Category.Fire:
                    result = "STR_statusEnflamedInfo";
                    break;
                case ProficiencyBase.Category.Ice:
                    result = "STR_statusFrozenInfo";
                    break;
                case ProficiencyBase.Category.LifeDrain:
                    result = "life steal";
                    break;
                case ProficiencyBase.Category.Lightning:
                    result = "STR_statusShockedInfo";
                    break;
                case ProficiencyBase.Category.Petrify:
                    result = "STR_statusPetrifiedInfo";
                    break;
                case ProficiencyBase.Category.Poison:
                    result = "STR_statusPoisonInfo";
                    break;
                case ProficiencyBase.Category.Protect:
                    result = "STR_statusProtectInfo";
                    break;
                case ProficiencyBase.Category.Reflect:
                    result = "STR_statusDamageReflectInfo";
                    break;
                case ProficiencyBase.Category.Scare:
                    result = "STR_statusFleeingInfo";
                    break;
                // case ProficiencyBase.Category.Shield:
                //     result = "";
                //     break;
                case ProficiencyBase.Category.Stunned:
                    result = "STR_statusStunnedInfo";
                    break;
                case ProficiencyBase.Category.Taunt:
                    result = "STR_skillsTauntInfo";
                    break;
                case ProficiencyBase.Category.Time:
                    if (prof.m_CustomValue > 0) result = "STR_statusSpedInfo";
                    else result = "STR_statusSlowedInfo";
                    break;
                case ProficiencyBase.Category.Water:
                    result = "STR_statusWetInfo";
                    break;
                case ProficiencyBase.Category.Evade:
                    if (prof.m_CustomValue > 0) result = "STR_statusEvadeUpInfo";
                    else result = "STR_statusEvadeDownInfo";
                    break;
                case ProficiencyBase.Category.ResistDeath:
                    result = "STR_statusResistDeathInfo";
                    break;
                case ProficiencyBase.Category.Resist:
                    if (prof.m_CustomValue > 0) result = "STR_statusResistUpInfo";
                    else result = "STR_statusResistDownInfo";
                    break;
                case ProficiencyBase.Category.Debuff:
                    result = "remove buffs";
                    break;
                case ProficiencyBase.Category.Rush:
                    result = "speed up next turn";
                    break;
                case ProficiencyBase.Category.StealItem: // incinerate
                    result = "item steal";
                    break;
                case ProficiencyBase.Category.Focus:
                    result = "remove focus points";
                    break;
                case ProficiencyBase.Category.Interrupt:
                    result = "";
                    break;
                case ProficiencyBase.Category.Cure:
                    result = "";
                    break;
                // case ProficiencyBase.Category.Darkness:
                //     result = "";
                //     break;
                // case ProficiencyBase.Category.StealGold:
                //     result = "";
                //     break;
                // case ProficiencyBase.Category.StealItem:
                //     result = "";
                //     break;
            }
            if (result == string.Empty) Plugin.Logger.LogError("no data for status effect " + prof.m_Category);
            if (!TextInfo.Instance.rowNames.Contains(result)) return result;
			return FTKHub.Localized<TextInfo>(result);
        }

        static string GetCurseDescription(CharacterStats.CurseType type)
        {
            return type switch
            {
                CharacterStats.CurseType.Blind => "STR_statusBlindInfo",
                CharacterStats.CurseType.Clumsy => "STR_statusClumsyInfo",
                CharacterStats.CurseType.Feeble => "STR_statusFeebleInfo",
                CharacterStats.CurseType.Foolish => "STR_statusFoolishInfo",
                CharacterStats.CurseType.Lethargic => "STR_statusLethargicInfo",
                CharacterStats.CurseType.Unlucky => "STR_statusUnluckyInfo",
                CharacterStats.CurseType.Unwell => "STR_statusUnwellInfo",
                _ => "",
            };
        }
    }
}