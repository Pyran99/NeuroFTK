using System.Collections;
using HarmonyLib;
using StartGameFE;
using UnityEngine;
using NeuroFTK.GameConfigs;
using System.Collections.Generic;
using GridEditor;

namespace NeuroFTK.HarmonyPatches.AutomatedActions
{
    /*TODO try making the custom rules a text json file that can be modified without rebuild*/
    [HarmonyPatch]
    public class SetCustomHouseRules
    {
        public static GameConfig configInstance;

        [HarmonyPatch(typeof(HouseRule), nameof(HouseRule.Show))]
        [HarmonyPostfix]
        static void OnRuleScreenShown(HouseRule __instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> ___m_Sliders)
        {
            Plugin.Logger.LogMessage("starting custom house rules");
            __instance.StartCoroutine(SetWithDelays(__instance, ___m_Sliders));

            static IEnumerator SetWithDelays(HouseRule instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> m_Sliders)
            {
                Plugin.Logger.LogMessage("Awaiting key input 'LeftBracket'");
                while (!Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    yield return null;
                }
                CustomRuleValues selectedRules = CustomHouseRules.houseRules[configInstance.GetCurrentGameDefPreview().m_SaveFileName];
                LogValues(selectedRules);

                yield return new WaitForSeconds(1.0f);
                instance.UpdateChaos(GetNormalizedValue(selectedRules.chaosFrequency, FTK_gameParams.ID.chaos));

                yield return new WaitForSeconds(1.0f);
                instance.UpdateLife(GetNormalizedValue(selectedRules.lifePool, FTK_gameParams.ID.lifepool));

                yield return new WaitForSeconds(1.0f);
                instance.UpdateInflation(GetNormalizedValue(selectedRules.economyInflation, FTK_gameParams.ID.inflation));

                foreach (KeyValuePair<FTK_gameParams.ID, HouseRuleSlider> _slider in m_Sliders)
                {
                    if (_slider.Key == FTK_gameParams.ID.deliver_gold)
                    {
                        yield return new WaitForSeconds(1.0f);
                        instance.UpdateGold(GetNormalizedValue(selectedRules.goldTarget, FTK_gameParams.ID.deliver_gold));
                        break;
                    }
                }
                Plugin.Logger.LogMessage("finished custom rules");
                yield return new WaitForSeconds(1.0f);
                instance.OnBack();
                yield return null;
            }
        }

        static float GetNormalizedValue(float value, FTK_gameParams.ID id)
        {
            FTK_gameParams gameParams = FTK_gameParamsDB.Get(id);
            float test = value * gameParams.m_SliderScale;
            if (id == FTK_gameParams.ID.inflation)
            {
                test = value / gameParams.m_SliderScale;
            }
            Plugin.Logger.LogMessage($"test *: {test}");
            Plugin.Logger.LogMessage($"scale {gameParams.m_SliderScale}");
            return test;
            // float min = gameParams.m_Min * gameParams.m_SliderScale; // HouseRules Show()
            // float max = gameParams.m_Max * gameParams.m_SliderScale;
            // float result = (value - min) / (max - min) * gameParams.m_SliderScale;
            // Plugin.Logger.LogMessage($"min: {min} max: {max} value: {value} result: {result}");
            // return result;
        }

        static void LogValues(CustomRuleValues rules)
        {
            Plugin.Logger.LogMessage($"chaos: {rules.chaosFrequency}");
            Plugin.Logger.LogMessage($"life: {rules.lifePool}");
            Plugin.Logger.LogMessage($"inflation: {rules.economyInflation}");
            Plugin.Logger.LogMessage($"gold: {rules.goldTarget}");
        }

        /* slider values
        Normalized is the percentage fill of the slider
        chaos->min-3 max-25
        life->min0 max-9
        inflation->min-3 max-15
        gold target->min-3 max-15
        */
    }
}