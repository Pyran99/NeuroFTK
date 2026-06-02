using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using StartGameFE;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MainMenuAction(MainScreen mainMenu, uiFTKButton newGame, uiFTKButton resumeGame, uiFTKButton spendLore) : NeuroAction<string>
    {
        readonly MainScreen mainMenu = mainMenu;
        readonly uiFTKButton newGame = newGame;
        readonly uiFTKButton resumeGame = resumeGame;
        readonly uiFTKButton spendLore = spendLore;

        public override string Name => "Main menu actions";

        protected override string Description => GetValidDescription();

        protected override JsonSchema Schema => GenerateSchema();

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage($"Executing main menu action {parsedData}");
            // switch ((string?)ActionData?.Data)
            // {
            //     case "new game":
            //         // MainMenu.OnNewGameAction(null);
            //         break;
            //     case "resume game":
            //         // MainMenu.OnResumeGameAction(null);
            //         break;
            //     case "spend lore":
            //         // MainMenu.OnSpendLoreAction(null);
            //         break;
            // }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogMessage("menu action data: " + actionData.Data);
            parsedData = (string)actionData.Data;
            return ExecutionResult.Success();
        }

        JsonSchema GenerateSchema()
        {
            Debug.Log("Generating schema");
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
            if (resumeGame != null) availableActions.Add("Resume a saved game.");
            if (spendLore != null) availableActions.Add("Spend lore points on unlocking various upgrades.");
            return availableActions;
        }

        string GetValidDescription()
        {
            string availableActions = "Start a new game.";
            if (resumeGame != null) availableActions += "Resume a saved game.";
            if (spendLore != null) availableActions += "Spend lore points on unlocking various upgrades.";
            return availableActions;
        }

        void OnMainScreenHide()
        {
            if (mainMenu.gameObject.activeSelf)
            {
                
            }
            mainMenu.gameObject.SetActive(false);
        }
    }
}