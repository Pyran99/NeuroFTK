using System.Collections;
using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
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
        public static readonly Dictionary<string, string> adventureCodes = new()
        {
            {"ftk", "For the King"},
            {"fa", "Frost Adventure"},
            {"id", "Into the Deep"},
            {"dc", "Dungeon Crawl"},
            {"hc", "Hildebrant's Cellar"},
            {"gr", "Gold Rush"},
        };

        static GameConfig instance;

        [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.Show))]
        [HarmonyPostfix]
        static void OnGameConfigShown(GameConfig __instance)
        {
            instance = __instance;
            SetCustomHouseRules.configInstance = __instance;
            if (__instance.m_IsResume)
            {
                ActionWindow window = ActionWindow.Create(__instance.gameObject);
                window.AddAction(new ResumeAdventureAction(__instance));
                window.SetForce(0, "resume your adventure", "you are in the adventure select screen", true);
                UnregisterDisabledObject.QuickCreate(__instance.gameObject, window);
                window.Register();
                return;
            }
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
            string details = "## Adventure details ";
            string description;
            foreach (GameDefButton btn in instance.m_GameDefButtons)
            {
                GameDefinitionBase prev = btn.GetPreview();
                // if (GlobalConfig.ForcedFirstAdventure())
                // {
                //     if (prev.GetDisplayName() == "For the King")
                //     {
                //         description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                //         details += $"- {prev.GetDisplayName()}: {description}\n";
                //         break;
                //     }
                //     continue;
                // }
                if (GlobalConfig.ForcedCustomAdventure())
                {
                    if (adventureCodes.ContainsKey(GlobalConfig.AdventureCode))
                    {
                        if (prev.GetDisplayName() == adventureCodes[GlobalConfig.AdventureCode])
                        {
                            description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                            details += $"- {prev.GetDisplayName()}: {description}\n";
                            break;
                        }
                    }
                    continue;
                }
                if (!FTK_dlcDB.HasDLCBySaveFileName(prev.m_SaveFileName)) continue;
                // gold rush is multiplayer only
                if (prev.m_ExcludeGameMode.Contains(GameLogic.GameMode.SinglePlayer)) continue;
                description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                details += $"- {prev.GetDisplayName()}: {description}\n";
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
            string name = level.GetDisplayName();
            string dungeonRooms = "";
            if (level.m_ModeType == GameDefinitionBase.ModeType.EndlessDungeon)
            {
                int? value = StatsAchievements.StatsAchievements.GetPlayerStatistic(FTK_statistic.ID.STAT_CELLAR_ROOM_COUNT).Value;
                dungeonRooms = $" (your highest room clear for this adventure is {value ?? 0})";
            }
            Context.Send($"you selected the adventure {name}: {level.GetDisplayInfoText()} {dungeonRooms}");
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

    internal class ResumeAdventureAction(GameConfig instance) : NeuroAction
    {
        public override string Name => "resume_game";
        protected override string Description => "resumes the last save";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            SelectButton.StartCoroutine(instance.m_CreateGame.GetComponent<uiFTKButton>());
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}