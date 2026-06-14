using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class ConfiguePartyAction : NeuroAction
    {
        public static void RegisterConfigurePartyActions(GameObject owner)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ConfiguePartyAction());
            window.AddAction(new ChoosePartyNamesAction());
            window.SetForce(5, "choose to randomize the classes of your party or give 3 names for them and begin the game", "you are at the character party creation screen");
            window.Register();
        }

        public override string Name => "randomize_party";
        protected override string Description => "randomize the classes of your party. you can choose names afterwards";
        protected override JsonSchema Schema => null;

        protected override void Execute() => SetupParty.NeuroRandomizeParty();

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class ChoosePartyNamesAction : NeuroAction<List<string>>
    {
        public override string Name => "choose_party_names";
        protected override string Description => "pick 3 names for your party members then begin the game";
        protected override JsonSchema Schema => GetSchema();

        readonly int min = 3;
        readonly int max = 16;


        protected override void Execute(List<string> parsedData)
        {
            SetupParty.NeuroSetCharacterNames(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out List<string> parsedData)
        {
            parsedData = [];
            JToken token = actionData.Data.SelectToken("names");
            Plugin.Logger.LogMessage(token);
            if (token == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("names"));
            List<string> test = [];
            foreach (JToken name in token)
            {
                if (name.Type != JTokenType.String) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("names"));
                // if (name.Value<string>() is null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("names"));
                test.Add(name.Value<string>());

            }
            List<string> result = token.ToObject<List<string>>();
            if (!result.Count().Equals(3))
            {
                return ExecutionResult.Failure("choose_party_names action requires 3 names, you sent " + result.Count());
            }
            List<string> build = [];
            foreach (string name in result.Select(v => v))
            {
                if (name.Length < min || name.Length > max)
                {
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
                Type = JsonSchemaType.Object,
                Required = ["names"],
                Properties = new()
                {
                    ["names"] = new()
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
                    }
                }
            };
            return schema;
        }
    }
}
