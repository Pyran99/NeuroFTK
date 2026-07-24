using System.Collections;
using System.Linq;
using System.Text;
using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

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
        static void NewCurse(CharacterStats.CurseType _type)
        {
            Plugin.Logger.LogWarning("testNewCurse NYI");
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

        static void StatusAppliedCtx(ProficiencyBase prof, CharacterDummy _dummy)
        {
            string statusName = prof.m_ProficiencyData.GetLocalizedDisplayName(); // used for spawned text
            string desc = GetCategoryDescription(prof);
            // Burning (Take frequent light damage) applied to Goblin Assassin
            if (_dummy.m_CharacterOverworld == null)
            {
                // enemy dummy doesnt have overworld
                statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format([statusName, desc, CombatUtils.GetEnemyName(_dummy as EnemyDummy)]));
                return;
            }
            statusCtx.AppendLine(StringMessages.StatusEffectApplied.Format([statusName, desc, CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)]));
            // AddToDummy is called here on prof base
        }

        static IEnumerator StatusAppliedWait()
        {
            yield return new WaitForEndOfFrame();
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
                return;
            }
            statusEndCtx.AppendLine(StringMessages.StatusEffectRemoved.Format([statusName, desc, CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)]));
            if (statusEndWaiting) return;
            statusEndWaiting = true;
            _dummy.StartCoroutine(StatusRemovedWait());
        }

        static IEnumerator StatusRemovedWait()
        {
            yield return new WaitForEndOfFrame();
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
                // case ProficiencyBase.Category.Cure:
                //     result = "";
                //     break;
                // case ProficiencyBase.Category.Curse:
                //     //TODO type of curse. blind, clumsy, feeble, foolish, lethargic, unlucky, unwell, 
                //     break;
                // case ProficiencyBase.Category.Darkness:
                //     result = "";
                //     break;
                case ProficiencyBase.Category.Dazed:
                    result = "STR_statusDazedInfo";
                    break;
                case ProficiencyBase.Category.Death:
                    result = "STR_statusDeathMarkedInfo";
                    break;
                case ProficiencyBase.Category.Disease:
                    result = "STR_statusDiseasedInfo";
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
                // case ProficiencyBase.Category.LifeDrain:
                //     result = "";
                //     break;
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
                // case ProficiencyBase.Category.StealGold:
                //     result = "";
                //     break;
                // case ProficiencyBase.Category.StealItem:
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
            }
            if (result == string.Empty) Plugin.Logger.LogError("no data for status effect " + prof.m_Category);
            if (!TextInfo.Instance.rowNames.Contains(result)) return result;
			return FTKHub.Localized<TextInfo>(result);
        }
    }
}