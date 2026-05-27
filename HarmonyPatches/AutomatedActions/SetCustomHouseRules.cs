using System.Collections;
using HarmonyLib;
using StartGameFE;
using UnityEngine;
using NeuroFTK.GameConfigs;
using System.Collections.Generic;
using GridEditor;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;

namespace NeuroFTK.HarmonyPatches.AutomatedActions
{
    /*Sets the custom difficulty sliders for any adventure
    The values can be customized in CustomHouseRules.json, located in the same directory as NeuroFTK.dll*/
    [HarmonyPatch]
    public class SetCustomHouseRules
    {
        // // B:/Games/Epic Games/ForTheKing
        // var dir = Path.GetDirectoryName(Application.dataPath);
        public static GameConfig configInstance;
        public static Dictionary<string, CustomRuleValues> customRules;

        [HarmonyPatch(typeof(HouseRule), nameof(HouseRule.Show))]
        [HarmonyPostfix]
        static void OnRuleScreenShown(HouseRule __instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> ___m_Sliders)
        {
            Plugin.Logger.LogMessage("starting custom house rules");
            LoadCustomRules();
            __instance.StartCoroutine(SetWithDelays(__instance, ___m_Sliders));

            static IEnumerator SetWithDelays(HouseRule instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> m_Sliders)
            {
                Plugin.Logger.LogMessage("Awaiting key input 'LeftBracket'");
                while (!Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    yield return null;
                }
                customRules ??= new Dictionary<string, CustomRuleValues>(CustomHouseRules.houseRules);
                CustomRuleValues selectedRules = customRules[configInstance.GetCurrentGameDefPreview().m_SaveFileName];
                LogValues(selectedRules);

                yield return new WaitForSeconds(0.5f);
                instance.UpdateChaos(GetScaledValue(selectedRules.chaosFrequency, FTK_gameParams.ID.chaos));

                yield return new WaitForSeconds(0.5f);
                instance.UpdateLife(GetScaledValue(selectedRules.lifePool, FTK_gameParams.ID.lifepool));

                yield return new WaitForSeconds(0.5f);
                instance.UpdateInflation(GetScaledValue(selectedRules.economyInflation, FTK_gameParams.ID.inflation));

                foreach (KeyValuePair<FTK_gameParams.ID, HouseRuleSlider> _slider in m_Sliders)
                {
                    if (_slider.Key == FTK_gameParams.ID.deliver_gold)
                    {
                        yield return new WaitForSeconds(0.5f);
                        instance.UpdateGold(GetScaledValue(selectedRules.goldTarget, FTK_gameParams.ID.deliver_gold));
                        break;
                    }
                }
                Plugin.Logger.LogMessage("finished custom rules");
                yield return new WaitForSeconds(1.0f);
                instance.OnBack();
                yield return null;
            }
        }

        static float GetScaledValue(float value, FTK_gameParams.ID id)
        {
            FTK_gameParams gameParams = FTK_gameParamsDB.Get(id);
            float result = value * gameParams.m_SliderScale;
            if (id == FTK_gameParams.ID.inflation)
            {
                result = value / gameParams.m_SliderScale;
            }
            return result;
        }

        static void LogValues(CustomRuleValues rules)
        {
            Plugin.Logger.LogMessage($"chaos: {rules.chaosFrequency}");
            Plugin.Logger.LogMessage($"life: {rules.lifePool}");
            Plugin.Logger.LogMessage($"inflation: {rules.economyInflation}");
            Plugin.Logger.LogMessage($"gold: {rules.goldTarget}");
        }

        static void LoadCustomRules()
        {
            if (customRules != null) return;
            // B:\Games\Epic Games\ForTheKing\BepInEx\plugins\NeuroFTK.dll
            var loc = Assembly.GetExecutingAssembly().Location.Replace("NeuroFTK.dll", "");
            if (File.Exists(Path.Combine(loc, "CustomHouseRules.json")))
            {
                string loadedJson = File.ReadAllText(Path.Combine(loc, "CustomHouseRules.json"));
                Dictionary<string, CustomRuleValues> json = JsonConvert.DeserializeObject<Dictionary<string, CustomRuleValues>>(loadedJson);
                foreach (KeyValuePair<string, CustomRuleValues> rule in CustomHouseRules.houseRules)
                {
                    if (!loadedJson.Contains(rule.Key))
                    {
                        json.Add(rule.Key, rule.Value);
                    }
                }
                customRules = json;
            }
            else
            {
                string jsonString = JsonConvert.SerializeObject(CustomHouseRules.houseRules, Formatting.Indented);
                File.WriteAllText(Path.Combine(loc, "CustomHouseRules.json"), jsonString);
                customRules = new Dictionary<string, CustomRuleValues>(CustomHouseRules.houseRules);
            }
        }
    }
}