using System.Collections.Generic;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.HarmonyPatches;
using StartGameFE;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ChooseAdventureAction(GameConfig _instance) : NeuroAction<string>
    {
        readonly GameConfig instance = _instance;
        List<string> validAdventures;

        public override string Name => "select_adventure";
        protected override string Description => "select the adventure to play";
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["adventure"],
                Properties = new()
                {
                    ["adventure"] = QJS.Enum(GetAdventureNames()),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            ConfigureAdventure.NeuroSelectAdventure(instance, parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data?.Value<string>("adventure");
            if (result.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("adventure"));
            if (!validAdventures.Contains(result))
            {
                Plugin.Logger.LogError($"could not find game def {result}");
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("adventure"));
            }
            parsedData = result;
            return ExecutionResult.Success();
        }

        List<string> GetAdventureNames()
        {
            if (instance == null)
            {
                Plugin.Logger.LogError("instance is null");
                return ["For the King"];
            }
            List<string> names = [];
            if (GlobalConfig.ForcedCustomAdventure())
            {
                names.Add(ConfigureAdventure.adventureCodes[GlobalConfig.AdventureCode]);
            }
            else
            {
                foreach (GameDefButton btn in instance.m_GameDefButtons)
                {
                    GameDefinitionBase prev = btn.GetPreview();
                    // if no dlc
                    if (!FTK_dlcDB.HasDLCBySaveFileName(prev.m_SaveFileName)) continue;
                    // gold rush is multiplayer only
                    if (prev.m_ExcludeGameMode.Contains(GameLogic.GameMode.SinglePlayer)) continue;
                    names.Add(prev.GetDisplayName());
                }
            }
            validAdventures = [.. names];
            return names;
        }
    }
}