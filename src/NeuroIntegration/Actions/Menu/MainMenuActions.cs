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
        protected override JsonSchema Schema => GetSchema();

        protected override void Execute(string parsedData)
        {
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
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("enum"));
            }
            parsedData = (string)actionData.Data;
            return ExecutionResult.Success();
        }

        JsonSchema GetSchema()
        {
            JsonSchema schema = QJS.Enum(GetAvailableChoices());
            schema.Required = ["enum"];
            return schema;
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

        public static ActionWindow RegisterAction(MainScreen instance, bool _resumeGame, bool _canSpendLore)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.AddAction(new MainMenuAction(instance, _resumeGame, _canSpendLore));
            window.SetContext("you are in the main menu");
            window.SetForce(5, "Begin the game or spend lore points if you can afford anything", "you are in the games main menu", true);
            window.Register();
            return window;
        }
    }
}