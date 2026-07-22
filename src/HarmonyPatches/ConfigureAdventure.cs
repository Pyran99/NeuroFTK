using System.Collections;
using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using StartGameFE;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
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
            SetCustomHouseRules.configInstance = __instance;
            CreateActionWindow(__instance);
        }

        static void CreateActionWindow(GameConfig instance)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            string context = AdventuresContext(instance);
            window.SetContext(context);
            window.AddAction(new ChooseAdventureAction(instance));
            window.SetForce(0, "select an adventure to play", "you are in the adventure select screen", true);
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
            string details = "[Adventure details] ";
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
                        details += $"name: {prev.GetDisplayName()}. description: {description}\n";
                        break;
                    }
                    continue;
                }
                if (!FTK_dlcDB.HasDLCBySaveFileName(prev.m_SaveFileName)) continue;
                // gold rush is multiplayer only
                if (prev.m_ExcludeGameMode.Contains(GameLogic.GameMode.SinglePlayer)) continue;
                description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                details += $"name: {prev.GetDisplayName()}. description: {description}\n";
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
                Context.Send($"there was an issue selecting the adventure {saveFileName}, try something else");
                OnGameConfigShown(instance);
                yield return null;
            }
            GameDefinitionBase level = instance.GetCurrentGameDefPreview();
            Context.Send($"you selected the adventure {level.GetDisplayName()}: {level.GetDisplayInfoText()}");
            yield return new WaitForSeconds(1.0f);
            SetDifficulty(instance);
            SetGameMode(instance);
            yield return new WaitForSeconds(1.0f);
            SetRulesBeforeStartGame(instance);
        }

        public static void CreateGame()
        {
            uiFTKButton btn = instance.gameObject.transform.Find("Background/ButtonRoot/HostGame").GetComponent<uiFTKButton>();
            SelectButton.StartCoroutine(btn, 1.0f);
        }

        // always choose apprentice for now
        static void SetDifficulty(GameConfig instance)
        {
            if(GlobalConfig.IsDebugMode()) LogDifficulties(instance);
            instance.m_Difficulty.value = 0;
        }

        // always choose solo for now
        static void SetGameMode(GameConfig instance)
        {
            if(GlobalConfig.IsDebugMode()) LogGameModes(instance);
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