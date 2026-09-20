using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            {"lc", "Lost Civilization"},
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
                window.SetForce(0, "resume your adventure", "", true);
                UnregisterDisabledObject.QuickCreate(__instance.gameObject, window);
                window.Register();
                return;
            }
            CreateActionWindow(__instance);
        }

        [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.OnChangeValueGameDef))]
        [HarmonyPostfix]
        static void GameDefChanged(GameConfig __instance, string _gameDefName)
        {
            string name = __instance.GetCurrentGameDefPreview().GetDisplayName();
            QuestHelper.currentAdventure = (QuestHelper.Adventure)Enum.Parse(typeof(QuestHelper.Adventure), adventureCodes.First(x => x.Value == name).Key);
        }

        static void CreateActionWindow(GameConfig instance)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            string context = AdventuresContext(instance);
            // window.SetContext(context);
            window.AddAction(new ChooseAdventureAction(instance));
            window.SetForce(0f, "select an adventure to play", context, true);
            UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
            window.Register();
        }

        static void OnActionCancelled(ActionWindow window)
        {
            UnityEngine.Object.Destroy(window);
            instance.OnBack();
        }

        public static void NeuroSelectAdventure(GameConfig instance, string name)
        {
            instance.StartCoroutine(SelectAdventureButton(instance, name));
        }

        static string AdventuresContext(GameConfig instance)
        {
            StringBuilder sb = new("## Adventure details \n");
            string description;
            foreach (GameDefButton btn in instance.m_GameDefButtons)
            {
                GameDefinitionBase prev = btn.GetPreview();
                if (GlobalConfig.ForcedCustomAdventure())
                {
                    if (adventureCodes.ContainsKey(GlobalConfig.AdventureCode))
                    {
                        if (prev.GetDisplayName() == adventureCodes[GlobalConfig.AdventureCode])
                        {
                            description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                            sb.AppendLine($"- {prev.GetDisplayName()}: {description}\n");
                            break;
                        }
                    }
                    continue;
                }
                if (!FTK_dlcDB.HasDLCBySaveFileName(prev.m_SaveFileName)) continue;
                // gold rush is multiplayer only
                // if (prev.m_ExcludeGameMode.Contains(GameLogic.GameMode.SinglePlayer)) continue; // also in action
                description = StringReplace.ReplaceNewLine(prev.GetDisplayInfoText());
                sb.AppendLine($"- {prev.GetDisplayName()}: {description}\n");
            }
            return sb.ToString();
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
            Context.Send($"you selected the adventure {name}: {level.GetDisplayInfoText()} {dungeonRooms}", true);
            // QuestHelper.currentAdventure = (QuestHelper.Adventure)Enum.Parse(typeof(QuestHelper.Adventure), adventureCodes.First(x => x.Value == saveFileName).Key);
            // Plugin.Logger.LogWarning($"set adventure to {QuestHelper.currentAdventure}");
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

        // always choose easiest for now
        static void SetDifficulty(GameConfig instance)
        {
            if(GlobalConfig.IsDebugMode()) LogDifficulties(instance);
            instance.m_Difficulty.value = 0;
        }

        
        static void SetGameMode(GameConfig instance)
        {
            if(GlobalConfig.IsDebugMode()) LogGameModes(instance);
            GameLogic.GameMode[] modes = instance.GetCurrentGameDefPreview().GetSupportedGameMode();
            // string[] supportedModes = GameDefinitionBase.GetSupportedGameModeString(instance.GetCurrentGameDefPreview().GetSupportedGameMode());
            int num;
            if (modes.Contains(GameLogic.GameMode.SinglePlayer)) num = Array.IndexOf(modes, GameLogic.GameMode.SinglePlayer);
            else
            {
                Plugin.Logger.LogWarning("no single player option");
                num = Array.IndexOf(modes, GameLogic.GameMode.LocalMultiplayer);
            }
            if (num == -1) Plugin.Logger.LogError("invalid game mode");
            instance.m_GameType.value = num;
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