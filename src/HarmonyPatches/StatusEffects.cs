using System.Collections;
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
        static void NewCurse()
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
                StatusAppliedCtx(FTK_proficiencyTableDB.Get(_prof[i]), __instance);
            }
            if (statusCtx.Length == 0) return;
            if (statusWaiting) return;
            statusWaiting = true;
            __instance.StartCoroutine(StatusAppliedWait());
        }

        [HarmonyPatch(typeof(ProficiencyBase), nameof(ProficiencyBase.End))] // dummy RemoveProf calls, all overrides call base
        [HarmonyPostfix]
        static void ProfEnd(ProficiencyBase __instance, CharacterDummy _dummy)
        {
            if (!_dummy.m_SufferingProficiencies.ContainsKey(__instance.m_Category)) return;
            StatusRemoveCtx(FTK_proficiencyTableDB.Get(__instance.m_ProficiencyID), _dummy);
        }

        static void StatusAppliedCtx(FTK_proficiencyTable table, CharacterDummy _dummy)
        {
            string statusName = table.GetLocalizedDisplayName();
            string desc = GetCategoryDescription(table);
            // Slowed (speed down) applied to Wolf
            if (_dummy.m_CharacterOverworld == null)
            {
                // enemy dummy doesnt have overworld
                statusCtx.AppendLine($"{statusName} ({desc}) applied to {CombatUtils.GetEnemyName(_dummy as EnemyDummy)}");
                return;
            }
            statusCtx.AppendLine($"{statusName} ({desc}) applied to {CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)}");
            // AddToDummy is called here on prof base
        }

        static IEnumerator StatusAppliedWait()
        {
            yield return new WaitForEndOfFrame();
            statusWaiting = false;
            Context.Send(statusCtx.ToString());
            statusCtx = new();
        }

        static void StatusRemoveCtx(FTK_proficiencyTable table, CharacterDummy _dummy)
        {
            string statusName = table.GetLocalizedDisplayName();
            string desc = GetCategoryDescription(table);
            if (_dummy.m_CharacterOverworld == null)
            {
                statusEndCtx.AppendLine($"{statusName} ({desc}) removed from {CombatUtils.GetEnemyName(_dummy as EnemyDummy)}");
                return;
            }
            statusEndCtx.AppendLine($"{statusName} ({desc}) removed from {CharacterData.GetCharacterName(_dummy.m_CharacterOverworld)}");
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
					if (proficiencyBase.m_CustomValue > 0) text = FTKHub.Localized<TextMisc>("STR_HudSpeedUp");
					else text = FTKHub.Localized<TextMisc>("STR_HudSpeedDown");
					return text;
				case ProficiencyBase.Category.Debuff:
					return FTKHub.Localized<TextMisc>("STR_profDebuff");
				case ProficiencyBase.Category.Attack:
					if (proficiencyBase.m_CustomValue > 0) text = FTKHub.Localized<TextMisc>("STR_profAttackUp");
					else text = FTKHub.Localized<TextMisc>("STR_profAttackDown");
					return text;
				case ProficiencyBase.Category.Armor:
					if (proficiencyBase.m_CustomValue > 0) text = FTKHub.Localized<TextMisc>("STR_profArmorUp");
					else text = FTKHub.Localized<TextMisc>("STR_profArmorDown");
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
					if (proficiencyBase.m_CustomValue > 0) text = FTKHub.Localized<TextMisc>("STR_profResistUp");
					else text = FTKHub.Localized<TextMisc>("STR_profResistDown");
					return text;
				case ProficiencyBase.Category.Evade:
					if (proficiencyBase.m_CustomValue > 0) text = FTKHub.Localized<TextMisc>("STR_profEvadeUp");
					else text = FTKHub.Localized<TextMisc>("STR_profEvadeDown");
					return text;
				case ProficiencyBase.Category.LifeDrain:
					return FTKHub.Localized<TextMisc>("STR_profLifeDrain");
				case ProficiencyBase.Category.Protect:
					if (table.m_Target == CharacterDummy.TargetType.Aoe)text = FTKHub.Localized<TextMisc>("STR_profProtect2");
					else text = FTKHub.Localized<TextMisc>("STR_profProtect");
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

        // // called often with any ui update. hover decision btns
        // static readonly Dictionary<string, List<GameObject>> immunities = [];
        // static readonly Dictionary<string, List<GameObject>> ailments = [];

        // [HarmonyPatch(typeof(uiPlayerMainHudStatus), nameof(uiPlayerMainHudStatus.SetStatusIcons))]
        // [HarmonyPostfix]
        // static void OnSetIcons(uiPlayerMainHudStatus __instance)
        // {
        //     // uiPlayerMainHud\DisplayRoot\playerMainHudStatus
        //     if (!GlobalConfig.gameInitialized) return;
        //     Plugin.Logger.LogWarning("testSetStatusIcons");
        //     uiPlayerMainHud hud = __instance.transform.parent.transform.parent.GetComponent<uiPlayerMainHud>();
        //     CharacterOverworld cow = hud.m_Cow;
        //     GameObject _immunities = __instance.transform.Find("immunities").gameObject;
        //     GameObject _ailments = __instance.transform.Find("aliments").gameObject;
        // }

    }
}