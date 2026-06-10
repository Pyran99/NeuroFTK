using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class ConfigueParty : NeuroAction
    {
        public override string Name => "randomize_party";
        protected override string Description => "randomize the classes of your party. you can choose names afterwards";
        protected override JsonSchema Schema => new();

        protected override void Execute() => SetupParty.NeuroRandomizeParty();

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }

        public static void RegisterConfigurePartyActions(GameObject owner)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ConfigueParty());
            window.AddAction(new ChoosePartyNames());
            window.SetForce(3, "choose to randomize the classes of your party or give 3 names for them and begin the game", "you are in the character party creation screen");
            window.Register();
        }
    }

    public class ChoosePartyNames : NeuroAction<List<string>>
    {
        public override string Name => "choose_party_names";
        protected override string Description => "pick 3 names for you party members then begin the game";
        protected override JsonSchema Schema => GetSchema();

        readonly int min = 3;
        readonly int max = 16;


        protected override void Execute(List<string> parsedData)
        {
            if (parsedData == null) return;
            SetupParty.NeuroSetCharacterNames(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out List<string> parsedData)
        {
            Plugin.Logger.LogMessage(actionData.Data);
            List<string> build = [];
            if (!actionData.Data.Count().Equals(3))
            {
                parsedData = null;
                return ExecutionResult.Failure("choose_party_names action requires 3 names, you sent " + actionData.Data.Count());
            }
            foreach (string name in actionData.Data.Select(v => (string)v))
            {
                if (name.Length < min || name.Length > max)
                {
                    parsedData = null;
                    return ExecutionResult.Failure($"name {name} is not between {min} and {max} characters long");
                }
                build.Add(name);
            }
            parsedData = build;
            return ExecutionResult.Success();
        }

        JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Array,
                MinItems = 3,
                MaxItems = 3,
                Items = new()
                {
                    Type = JsonSchemaType.String,
                    MinLength = min,
                    MaxLength = max
                }
            };
            return schema;
        }
    }
}
