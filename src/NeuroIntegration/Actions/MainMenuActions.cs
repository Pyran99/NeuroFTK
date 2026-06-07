using System;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class MainMenuAction(MainScreen mainMenu, bool _resumeGame, bool _canSpendLore) : NeuroAction<string>
    {
        readonly MainScreen mainScreen = mainMenu;
        readonly bool resumeGame = _resumeGame;
        readonly bool spendLore = _canSpendLore;
        public Action<string> ButtonSelected { get; set; }


        public override string Name => "main_menu";

        protected override string Description => GetValidDescription();

        protected override JsonSchema Schema => GenerateSchema();

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage($"Executing main menu action {parsedData}");
            switch (parsedData)
            {
                case "new game":
                    SelectedButton(MainMenu.newGameBtn);
                    break;
                case "resume game":
                    SelectedButton(MainMenu.resumeBtn);
                    break;
                case "spend lore":
                    SelectedButton(MainMenu.loreBtn);
                    break;
                default:
                    Plugin.Logger.LogError($"invalid main menu action '{parsedData}'");
                    break;
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
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
            if (resumeGame) availableActions.Add("resume game");
            if (spendLore) availableActions.Add("spend lore");
            return availableActions;
        }

        string GetValidDescription()
        {
            string availableActions = "Choose to start a new game. ";
            if (resumeGame) availableActions += "Resume a saved game. ";
            if (spendLore) availableActions += "Spend lore points on unlocking various upgrades. ";
            return availableActions;
        }

        void SelectedButton(uiFTKButton button)
        {
            SelectButton.StartCoroutine(mainScreen, button, 1.0f);
        }
    }
}