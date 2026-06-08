using System.Collections;
using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;
using StartGameFE;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    [HarmonyPatch]
    public class ConfigureAdventure
    {
        // select adventure button, set house rules then auto start or send neuro action to start
        [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.Show))]
        [HarmonyPostfix]
        static void OnGameConfigShown(GameConfig __instance)
        {
            __instance.StartCoroutine(Wait(__instance));
            static IEnumerator Wait(GameConfig instance)
            {
                yield return new WaitForSeconds(2.0f);
                SelectAdventure(instance);
                yield return new WaitForSeconds(2.0f);
                SetDifficulty(instance);
                SetGameMode(instance);
                yield return new WaitForSeconds(2.0f);
                SetRulesBeforeStartGame(instance);
                yield return new WaitForSeconds(2.0f);
                GameDefinitionBase level = instance.GetCurrentGameDefPreview();
                Context.Send($"Selected the adventure '{level.GetDisplayName()}'. The adventures description is '{level.GetDisplayInfoText()}'", false);
                Plugin.Logger.LogMessage("NYI allow neuro to respond to adventure context & send action to move to party setup");
            }
        }

        static void NeuroSelectAdventure(GameConfig instance)
        {
            //TODO create action
        }

        static void SelectAdventure(GameConfig instance)
        {
            /*gold rush(GraveRobber) is co-op only
            LostCiv is dlc
            Cellar is journeyman only*/
            List<string> names = [];
            instance.m_GameDefButtons.ForEach(btn => names.Add(btn.m_GameDefName));
            List<string> search = [.. names];
            foreach (string item in search)
            {
                if (FTK_dlcDB.GetDLCBySaveFileName(item) != null)
                {
                    if (!FTK_dlcDB.GetDLCBySaveFileName(item).IsPurchased())
                    {
                        names.Remove(item);
                    }
                }
                if (!CustomHouseRules.houseRules.ContainsKey(item))
                {
                    names.Remove(item);
                }
            }
            Plugin.Logger.LogMessage($"valid names: {string.Join(", ", [.. names])}");
            string chosen = names[Random.Range(0, names.Count)];
            if (!GameCache.Cache.GameDefinitions.GetNames().Contains(chosen))
            {
                Plugin.Logger.LogWarning($"could not find game def {chosen}, defaulting to KillVexor");
                Context.Send($"{chosen} is an invalid adventure, defaulting to For the King");
                chosen = "KillVexor";
            }
            SelectAdventureButton(instance, chosen);
        }

        static void SelectAdventureButton(GameConfig instance, string saveFileName)
        {
            bool invalid = true;
            for (int i = 0; i < instance.m_GameDefButtons.Count; i++)
            {
                if (instance.m_GameDefButtons[i].m_GameDefName == saveFileName)
                {
                    Plugin.Logger.LogMessage($"selected btn for {saveFileName}");
                    instance.m_GameDefButtons[i].OnClick();
                    invalid = false;
                    break;
                }
            }
            if (invalid)
            {
                Plugin.Logger.LogWarning($"could not find game def {saveFileName}, defaulting to KillVexor");
                SelectAdventureButton(instance, "KillVexor");
            }
        }

        // always choose apprentice for now
        static void SetDifficulty(GameConfig instance)
        {
            LogDifficulties(instance);
            instance.m_Difficulty.value = 0;
        }

        // always choose solo for now
        static void SetGameMode(GameConfig instance)
        {
            LogGameModes(instance);
            instance.m_GameType.value = 0;
        }

        static void SetRulesBeforeStartGame(GameConfig instance)
        {
            // bool useCustomRules = CustomHouseRules.SET_CUSTOM_RULES;
            // if (!useCustomRules) return;
            SetCustomHouseRules.configInstance = instance;
            instance.OnHouseRule();
        }

        static void LogDifficulties(GameConfig instance)
        {
            GameDefinitionPreview selected = instance.GetCurrentGameDefPreview();
            foreach (KeyValuePair<GameDifficulty.DifficultyType, GameDifficulty> item in selected?.m_GameDifficulties)
            {
                Plugin.Logger.LogMessage($"{item.Key}: {item.Value.m_DisplayName}");
                // Easy: STR_buttonEasy
                // Medium: STR_buttonNormal
                // Hard: STR_buttonHard
            }
        }

        static void LogGameModes(GameConfig instance)
        {
            GameDefinitionPreview selected = instance.GetCurrentGameDefPreview();
            string[] modes = GameDefinitionBase.GetSupportedGameModeString(selected.GetSupportedGameMode());
            Plugin.Logger.LogMessage($"game modes: {string.Join(", ", modes)}");
            //game modes: Solo Adventure, Online Co-Op, Local Co-Op
        }
    }
}