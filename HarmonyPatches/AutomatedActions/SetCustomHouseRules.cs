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

namespace NeuroFTK.HarmonyPatches.AutomatedActions;
/*Sets the custom difficulty sliders for any adventure
The values can be customized in CustomHouseRules.json, located in the same directory as NeuroFTK.dll*/
[HarmonyPatch]
public class SetCustomHouseRules
{
    // // B:/Games/Epic Games/ForTheKing
    // var dir = Path.GetDirectoryName(Application.dataPath);
    readonly static string rulesConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NeuroFTKCustomHouseRules.json");
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
            Plugin.Logger.LogMessage("Awaiting key input 'LeftBracket'"); // PH before other option implemented
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
            Plugin.Logger.LogMessage("Applied custom rules");
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
        Plugin.Logger.LogMessage($"chaos: {rules.chaosFrequency}\nlife: {rules.lifePool}\ninflation: {rules.economyInflation}\ngold: {rules.goldTarget}");
    }

    public static void LoadCustomRules()
    {
        if (customRules != null) return;
        if (File.Exists(rulesConfigPath))
        {
            string loadedJson = File.ReadAllText(rulesConfigPath);
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
            File.WriteAllText(rulesConfigPath, jsonString);
            customRules = new Dictionary<string, CustomRuleValues>(CustomHouseRules.houseRules);
        }
    }
}