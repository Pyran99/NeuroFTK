using System;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.HarmonyPatches;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MainMenuAction(MainScreen mainMenu, bool _resumeGame, bool _canSpendLore) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(MainScreen instance, bool _resumeGame, bool _canSpendLore)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.AddAction(new MainMenuAction(instance, _resumeGame, _canSpendLore));
            window.SetForce(5, "Begin the game or spend lore points if you can afford anything", "you are at the games main menu", true);
            window.Register();
            return window;
        }

        readonly MainScreen mainScreen = mainMenu;
        readonly bool resumeGame = _resumeGame;
        readonly bool spendLore = _canSpendLore;
        public Action<string> ButtonSelected { get; set; }

        public override string Name => "main_menu";
        protected override string Description => GetValidDescription();
        protected override JsonSchema Schema => GetSchema();

        JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["action"],
                Properties = new()
                {
                    ["action"] = QJS.Enum(GetAvailableChoices())
                }
            };
            return schema;
        }

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
            string result = actionData.Data.Value<string>("action");
            bool present = choices.Contains(result);
            if (!present)
            {
                parsedData = null;
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("action"));
            }
            parsedData = result;
            return ExecutionResult.Success();
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
            SelectButton.StartCoroutine(button, 1.0f);
        }
    }
}