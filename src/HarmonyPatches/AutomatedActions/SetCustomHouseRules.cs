using System.Collections;
using HarmonyLib;
using StartGameFE;
using UnityEngine;
using Pyran.NeuroFTK.GameConfigs;
using System.Collections.Generic;
using GridEditor;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Pyran.NeuroFTK.NeuroIntegration;
using System.Linq;

namespace Pyran.NeuroFTK.HarmonyPatches;
/*Sets the custom difficulty sliders for any adventure
The values can be customized in NeuroFTKCustomHouseRules.json, located in the same directory as NeuroFTK.dll*/
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
        Plugin.Logger.LogMessage($"setting house rules with `{CustomHouseRules.SET_CUSTOM_RULES}` custom rules");
        LoadCustomRules();

        __instance.StartCoroutine(SetWithDelays(__instance, ___m_Sliders));

        static IEnumerator SetWithDelays(HouseRule instance, Dictionary<FTK_gameParams.ID, HouseRuleSlider> m_Sliders)
        {
            customRules ??= new Dictionary<string, CustomRuleValues>(CustomHouseRules.houseRules);
            if (configInstance == null)
            {
                Plugin.Logger.LogError("null GameConfig");
                yield break;
            }
            GameDefinitionPreview prev = configInstance.GetCurrentGameDefPreview();
            if (prev == null)
            {
                configInstance.m_GameDefButtons.First()?.OnControllerClick();
                prev = configInstance.m_GameDefButtons.First().GetPreview();
            }
            CustomRuleValues selectedRules = customRules[prev.m_SaveFileName];
            LogValues(selectedRules);

            yield return new WaitForSeconds(0.3f);
            instance.UpdateChaos(GetScaledValue(selectedRules.chaosFrequency, FTK_gameParams.ID.chaos));

            yield return new WaitForSeconds(0.3f);
            instance.UpdateLife(GetScaledValue(selectedRules.lifePool, FTK_gameParams.ID.lifepool));

            yield return new WaitForSeconds(0.3f);
            instance.UpdateInflation(GetScaledValue(selectedRules.economyInflation, FTK_gameParams.ID.inflation));

            foreach (KeyValuePair<FTK_gameParams.ID, HouseRuleSlider> _slider in m_Sliders)
            {
                if (_slider.Key == FTK_gameParams.ID.deliver_gold)
                {
                    yield return new WaitForSeconds(0.3f);
                    instance.UpdateGold(GetScaledValue(selectedRules.goldTarget, FTK_gameParams.ID.deliver_gold));
                    break;
                }
            }
            Plugin.Logger.LogMessage("Applied custom rules");
            // if (GlobalConfig.IsDebugMode())
            // {
            //     Plugin.Logger.LogWarning("Awaiting key input 'RightBracket' to close. from debugMode");
            //     while (!Input.GetKeyDown(KeyCode.RightBracket))
            //     {
            //         yield return null;
            //     }
            // }
            instance.OnBack();
            ConfigureAdventure.CreateGame();
        }
    }

    /// <summary>
    /// chaos-1, life-1, inflation-10, gold-0.04
    /// </summary>
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

    static void LogValues(CustomRuleValues rules) => Plugin.Logger.LogMessage($">chaos: {rules.chaosFrequency} >life: {rules.lifePool} >inflation: {rules.economyInflation} >gold: {rules.goldTarget}");

    public static void LoadCustomRules()
    {
        if (CustomHouseRules.SET_CUSTOM_RULES)
        {
            if (File.Exists(rulesConfigPath))
            {
                string loadedJson = File.ReadAllText(rulesConfigPath);
                Dictionary<string, CustomRuleValues> json = JsonConvert.DeserializeObject<Dictionary<string, CustomRuleValues>>(loadedJson);
                bool keyAdded = false;
                foreach (KeyValuePair<string, CustomRuleValues> rule in CustomHouseRules.houseRules)
                {
                    if (!loadedJson.Contains(rule.Key))
                    {
                        json.Add(rule.Key, rule.Value);
                        keyAdded = true;
                    }
                }
                if (keyAdded)
                {
                    string jsonString = JsonConvert.SerializeObject(json, Formatting.Indented);
                    File.WriteAllText(rulesConfigPath, jsonString);
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
        else
        {
            customRules = new Dictionary<string, CustomRuleValues>(CustomHouseRules.houseRules);
        }
    }
}