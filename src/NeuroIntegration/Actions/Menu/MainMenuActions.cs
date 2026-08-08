using System;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using StartGameFE;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class MainMenuAction(MainScreen mainMenu, IEnumerable<string> _choices) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(MainScreen instance, IEnumerable<string> _choices)
        {
            ActionWindow window = ActionWindow.Create(instance.gameObject);
            window.AddAction(new MainMenuAction(instance, _choices));
            window.SetForce(0, "Begin the game or spend lore points if you can afford anything", "you are at the games main menu", true);
            window.Register();
            return window;
        }

        readonly MainScreen mainScreen = mainMenu;
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
                    ["action"] = QJS.Enum(_choices)
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            MainMenu.NeuroDecision(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            string result = actionData.Data.Value<string>("action");
            bool present = _choices.Contains(result);
            if (!present)
            {
                parsedData = null;
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("action"));
            }
            parsedData = result;
            return ExecutionResult.Success();
        }

        string GetValidDescription()
        {
            string availableActions = "Choose to start a new game. ";
            if (_choices.Contains("resume game")) availableActions += "Resume a saved game. ";
            if (_choices.Contains("spend lore")) availableActions += "Spend lore points on unlocking various upgrades. ";
            return availableActions;
        }
    }
}