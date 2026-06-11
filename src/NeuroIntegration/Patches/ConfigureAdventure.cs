using System.Collections;
using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
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
        static GameConfig instance;

        [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.Show))]
        [HarmonyPostfix]
        static void OnGameConfigShown(GameConfig __instance)
        {
            instance = __instance;
            CreateActionWindow(__instance);
        }

        static void CreateActionWindow(GameConfig instance)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            string context = AdventuresContext(instance);
            window.SetContext(context);
            window.AddAction(new ChooseAdventure(instance));
            window.SetForce(5, "select an adventure to play", "you are in the adventure select screen", true);
            UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
            window.Register();
        }

        static void OnActionCancelled(ActionWindow window)
        {
            Object.Destroy(window);
            instance.OnBack();
        }

        public static void NeuroSelectAdventure(GameConfig instance, string name)
        {
            instance.StartCoroutine(SelectAdventureButton(instance, name));
        }

        static string AdventuresContext(GameConfig instance)
        {
            string details = "Adventure details: ";
            bool forceFirst = (bool)Plugin.config["force_first_adventure"];
            string description;
            foreach (GameDefButton btn in instance.m_GameDefButtons)
            {
                GameDefinitionBase prev = btn.GetPreview();
                if (forceFirst)
                {
                    if (prev.GetDisplayName() == "For the King")
                    {
                        description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                        details += $"{{name: {prev.GetDisplayName()}, description: {description}}}; ";
                        break;
                    }
                    continue;
                }
                if (!FTK_dlcDB.HasDLCBySaveFileName(prev.m_SaveFileName)) continue;
                // gold rush is multiplayer only
                if (prev.m_ExcludeGameMode.Contains(GameLogic.GameMode.SinglePlayer)) continue;
                description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                details += $"{{name: {prev.GetDisplayName()}, description: {description}}}; ";
            }
            return details;
        }

        static IEnumerator SelectAdventureButton(GameConfig instance, string saveFileName)
        {
            bool invalid = true;
            foreach (GameDefButton btn in instance.m_GameDefButtons)
            {
                string shownName = btn.GetPreview().GetDisplayName();
                if (shownName == saveFileName)
                {
                    btn.OnClick();
                    invalid = false;
                    break;
                }
            }
            if (invalid)
            {
                Plugin.Logger.LogError($"could not find game def {saveFileName}");
                Context.Send($"there was an issue selecting the adventure {saveFileName}");
                OnGameConfigShown(instance);
                yield return null;
            }
            GameDefinitionBase level = instance.GetCurrentGameDefPreview();
            Context.Send($"Selected the adventure '{level.GetDisplayName()}', '{level.GetDisplayInfoText()}'; please wait while it is being setup");
            yield return new WaitForSeconds(0.5f);
            SetDifficulty(instance);
            SetGameMode(instance);
            yield return new WaitForSeconds(0.5f);
            SetRulesBeforeStartGame(instance);
        }

        public static void CreateGame()
        {
            uiFTKButton btn = instance.gameObject.transform.Find("Background/ButtonRoot/HostGame").GetComponent<uiFTKButton>();
            SelectButton.StartCoroutine(instance, btn, 1.0f);
        }

        // always choose apprentice for now
        static void SetDifficulty(GameConfig instance)
        {
            if(GlobalConfig.debug_mode) LogDifficulties(instance);
            instance.m_Difficulty.value = 0;
        }

        // always choose solo for now
        static void SetGameMode(GameConfig instance)
        {
            if(GlobalConfig.debug_mode) LogGameModes(instance);
            instance.m_GameType.value = 0;
        }

        static void SetRulesBeforeStartGame(GameConfig instance)
        {
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