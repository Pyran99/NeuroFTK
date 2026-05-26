using System.Collections;
using HarmonyLib;
using StartGameFE;
using UnityEngine;
using NeuroFTK.GameConfigs;
using System.Collections.Generic;
using GridEditor;

namespace NeuroFTK.HarmonyPatches.AutomatedActions
{
    [HarmonyPatch]
    public class SetCustomHouseRules
    {

        //FIXME these dont work as expected
        [HarmonyPatch(typeof(HouseRule), nameof(HouseRule.Show))]
        [HarmonyPostfix]
        static void OnRuleScreenAwake(HouseRule __instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> ___m_Sliders)
        {
            Plugin.Logger.LogMessage("starting custom house rules");
            __instance.StartCoroutine(SetWithDelays(__instance, ___m_Sliders));

            static IEnumerator SetWithDelays(HouseRule instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> m_Sliders)
            {
                yield return new WaitForSeconds(1.0f);
                instance.UpdateChaos(CustomHouseRules.CHAOS_FREQUENCY);
                Plugin.Logger.LogMessage($"chaos: {CustomHouseRules.CHAOS_FREQUENCY}");
                yield return new WaitForSeconds(1.0f);
                Plugin.Logger.LogMessage($"life: {CustomHouseRules.LIFE_POOL}");
                instance.UpdateLife(CustomHouseRules.LIFE_POOL);
                yield return new WaitForSeconds(1.0f);
                Plugin.Logger.LogMessage($"inflation: {CustomHouseRules.ECONOMY_INFLATION}");
                instance.UpdateInflation(CustomHouseRules.ECONOMY_INFLATION);
                foreach (KeyValuePair<FTK_gameParams.ID, HouseRuleSlider> slider in m_Sliders)
                {
                    if (slider.Key == FTK_gameParams.ID.deliver_gold)
                    {
                        yield return new WaitForSeconds(1.0f);
                        Plugin.Logger.LogMessage($"gold: {CustomHouseRules.GOLD_TARGET}");
                        instance.UpdateGold(CustomHouseRules.GOLD_TARGET);
                        break;
                    }
                }
                Plugin.Logger.LogMessage("finished custom rules");
                yield return new WaitForSeconds(1.0f);
                instance.OnBack(); //FIXME might break game
                yield return null;
            }
        }

    }
}