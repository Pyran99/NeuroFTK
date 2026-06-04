using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class MainMenuAction(MainScreen mainMenu, uiFTKButton resumeGame, bool canSpendLore, string name) : NeuroAction<string>
    {
        readonly MainScreen mainMenu = mainMenu;
        readonly uiFTKButton resumeGame = resumeGame;
        readonly bool spendLore = canSpendLore;

        public override string Name => name;
        // public override string Name => "main menu actions";

        protected override string Description => GetValidDescription();

        protected override JsonSchema Schema => GenerateSchema();

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage($"Executing main menu action {parsedData}");
            switch (parsedData)
            {
                case "new game":
                    StartNewGame();
                    break;
                case "resume game":
                    ResumeLastSave();
                    // resumeGame?.OnControllerClick();
                    // mainMenu.OnResume();
                    break;
                case "spend lore":
                    OpenLoreStore();
                    break;
                default:
                    Plugin.Logger.LogError($"invalid main menu action {parsedData}");
                    break;
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogMessage("main menu action data: " + actionData.Data);
            IEnumerable<string> choices = GetAvailableChoices();
            bool present = choices.Contains((string)actionData.Data);
            if (!present)
            {
                parsedData = null;
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("choices"));
            }
            parsedData = (string)actionData.Data;
            return ExecutionResult.Success();
        }

        JsonSchema GenerateSchema()
        {
            return new JsonSchema
            {
                Type = JsonSchemaType.String,
                Required = ["choices"],
                Properties = new Dictionary<string, JsonSchema>
                {
                    ["choices"] = QJS.Enum(GetAvailableChoices())
                }
            };
        }

        IEnumerable<string> GetAvailableChoices()
        {
            List<string> availableActions = ["new game"];
            if (resumeGame != null && resumeGame.enabled) availableActions.Add("resume saved game.");
            if (spendLore) availableActions.Add("spend lore");
            return availableActions;
        }

        string GetValidDescription()
        {
            string availableActions = "Start a new game. ";
            if (resumeGame != null && resumeGame.enabled) availableActions += "Resume a saved game. ";
            if (spendLore) availableActions += "Spend lore points on unlocking various upgrades. ";
            return availableActions;
        }

        void StartNewGame()
        {
            Plugin.Logger.LogMessage("new game press");
            mainMenu.OnNewGame();
        }

        void OpenLoreStore()
        {
            Plugin.Logger.LogMessage("spend lore press");
            mainMenu.ShowLoreStore();
        }

        void ResumeLastSave()
        {
            Plugin.Logger.LogMessage("resume match press");
            mainMenu.OnResume();
        }
    }
}