using System.Collections.Generic;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class ChooseAdventure(GameConfig _instance) : NeuroAction<string>
    {
        readonly GameConfig instance = _instance;
        List<string> validAdventures;

        public override string Name => "select_adventure";
        protected override string Description => "select the map to play";
        protected override JsonSchema Schema => QJS.Enum(GetAdventureNames());

        protected override void Execute(string parsedData)
        {
            ConfigureAdventure.NeuroSelectAdventure(instance, parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            if (!validAdventures.Contains((string)actionData.Data))
            {
                Plugin.Logger.LogWarning($"could not find game def {(string)actionData.Data}");
                parsedData = "";
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("enum"));
            }
            Plugin.Logger.LogMessage($"chosen adventure {actionData.Data}");
            parsedData = (string)actionData.Data;
            return ExecutionResult.Success();
        }

        List<string> GetAdventureNames()
        {
            if (instance == null)
            {
                Plugin.Logger.LogError("instance is null");
                return [];
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
    }
}