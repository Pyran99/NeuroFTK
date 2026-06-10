using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class ChoosePartyNames : NeuroAction<List<string>>
    {
        public override string Name => "choose_names";
        protected override string Description => "pick 3 names for you party members";
        protected override JsonSchema Schema => GetSchema();

        readonly int min = 3;
        readonly int max = 16;

        protected override ExecutionResult Validate(ActionJData actionData, out List<string> parsedData)
        {
            Plugin.Logger.LogMessage(actionData.Data);
            if (actionData.Data.Count() != 3)
            {
                parsedData = null;
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(["player1", "player2", "player3"]));
            }
            List<string> build = [];
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

        protected override void Execute(List<string> parsedData)
        {
            SetupParty.NeuroSetCharacterNames(parsedData);
        }


        JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["player1", "player2", "player3"],
                Properties = new Dictionary<string, JsonSchema>
                {
                    ["player1"] = new()
                    {
                        Type = JsonSchemaType.String,
                        MinLength = min,
                        MaxLength = max,
                    },
                    ["player2"] = new()
                    {
                        Type = JsonSchemaType.String,
                        MinLength = min,
                        MaxLength = max,
                    },
                    ["player3"] = new()
                    {
                        Type = JsonSchemaType.String,
                        MinLength = min,
                        MaxLength = max,
                    },
                },
            };
            return schema;
        }

        public static void RegisterAction(GameObject owner)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ChoosePartyNames());
            window.SetForce(0, "pick 3 names for you party members", "you are in the character party creation screen");
            window.Register();
        }
    }

}
