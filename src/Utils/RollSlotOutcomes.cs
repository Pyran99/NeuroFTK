using System.Collections.Generic;
using Google2u;
using GridEditor;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class RollSlotOutcomes
    {
        // { 0: {5%: failure} }
        public static Dictionary<string, Dictionary<string, string>> GetOutcomes(FTK_slotOutput.ID _id)
        {
            Dictionary<string, Dictionary<string, string>> result = [];
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            FTK_slotOutput entry = FTK_slotOutputDB.GetDB().GetEntry(_id);
            FTK_progressionTier.ID progID = FTK_progressionTier.ID.None;
            FTK_weaponStats2.SkillType _skill = entry.m_TestSkill;
            FTK_slotOutput entry2 = FTK_slotOutputDB.GetDB().GetEntry(_id);
            string displayTitle = FTKHub.Localized<TextMisc>(entry2.m_LegendTitle);
            int slotAmount = entry2.m_SlotAmount + 1;
            uiSlotLegend.SlotOutput[] outputs = new uiSlotLegend.SlotOutput[slotAmount];

            for (int i = 0; i < outputs.Length; i++)
            {
                outputs[i] = uiSlotLegend.Instance.GetSlotOutputResult(i, _id, progID);
            }
            float skillMod = 0f;
            if (cow.m_HexLand.m_POI)
            {
                skillMod = cow.m_HexLand.m_POI.GetSkillModifier(_id, cow);
            }
            float skillRoll = cow.m_CharacterStats.GetSkillValue(_skill, true, skillMod);
            // DisplayEachOutput
            for (int i = 0; i < outputs.Length; i++)
            {
                if (!result.ContainsKey(i.ToString())) result[i.ToString()] = [];
                float percent = 0f;
                if (i >= 0) // assumes no spend focus, only give base values
                {
                    int num2 = outputs.Length - 1;
                    int num3 = i - 0;
                    percent = Combination(num2, num3) * Mathf.Pow(skillRoll, num3) * Mathf.Pow(1f - skillRoll, num2 - num3);
                }
                string name = outputs[i].GetDisplayName(false); // Failure | Success
                Dictionary<string, string> data = new()
                {
                    { FTKUtil.RoundToInt(percent * 100f).ToString() + "%", name },
                };
                result[i.ToString()] = data;
            }
            return result;
        }

        private static float Combination(int _n, int _k)
        {
            return Factorial(_n) / (float)(Factorial(_k) * Factorial(_n - _k));
        }

        private static int Factorial(int i)
        {
            if (i <= 1)
            {
                return 1;
            }
            return i * Factorial(i - 1);
        }


        public static FTK_slotOutput.ID _getAmbushType(MiniHexEnemy _mhe, CharacterOverworld _cow)
        {
            FTK_slotOutput.ID id = FTK_slotOutput.ID.ambush;
            if (_cow.m_CharacterStats.m_CharacterSkills.m_Ambush)
            {
                id = FTK_slotOutput.ID.skilledAmbush;
                if (_mhe is MiniHexEnemyCamp)
                {
                    id = FTK_slotOutput.ID.ambush;
                }
            }
            else if (_mhe is MiniHexEnemyCamp)
            {
                id = FTK_slotOutput.ID.campAmbush;
            }
            return id;
        }

        public static FTK_slotOutput.ID _getSneakType(MiniHexEnemy _mhe, CharacterOverworld _cow)
        {
            if (_cow.m_CharacterStats.m_ActionPoints == 0)
            {
                return FTK_slotOutput.ID.None;
            }
            if (!_mhe.m_HexLand.HasSneakHex())
            {
                return FTK_slotOutput.ID.None;
            }
            FTK_slotOutput.ID id = FTK_slotOutput.ID.sneak;
            if (_cow.m_CharacterStats.m_CharacterSkills.m_Sneak || _cow.m_CharacterStats.IsGhost)
            {
                id = FTK_slotOutput.ID.skilledSneak;
                if (_mhe is MiniHexEnemyCamp)
                {
                    id = FTK_slotOutput.ID.sneak;
                }
            }
            else if (_mhe is MiniHexEnemyCamp)
            {
                id = FTK_slotOutput.ID.campSneak;
            }
            return id;
        }

    }
}