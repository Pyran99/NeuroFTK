using System.Collections.Generic;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class ChooseAdventureAction(GameConfig _instance) : NeuroAction<string>
    {
        readonly GameConfig instance = _instance;
        List<string> validAdventures;

        public override string Name => "select_adventure";
        protected override string Description => "select the adventure to play";
        protected override JsonSchema Schema => GetSchema();

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage($"chosen adventure {parsedData}");
            ConfigureAdventure.NeuroSelectAdventure(instance, parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data.Value<string>("adventure");
            if (result == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("adventure"));
            if (!validAdventures.Contains(result))
            {
                Plugin.Logger.LogWarning($"could not find game def {result}");
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
            if ((bool)Plugin.config["force_first_adventure"] == true) names.Add("For the King");
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
            Plugin.Logger.LogMessage($"valid adventures: {string.Join(", ", [.. names])}");
            validAdventures = [.. names];
            return names;
        }

        JsonSchema GetSchema()
        {
            // JsonSchema schema = QJS.Enum(GetAdventureNames());
            // schema.Type = JsonSchemaType.Object;
            // schema.Required = ["enum"];
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
    }
}