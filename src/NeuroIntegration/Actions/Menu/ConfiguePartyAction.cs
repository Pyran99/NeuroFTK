using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.HarmonyPatches;
using UnityEngine;
using System.Text;
using GridEditor;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ConfiguePartyAction : NeuroAction
    {
        public static void RegisterConfigurePartyActions(GameObject owner, List<uiQuickPlayerCreate> players)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ConfiguePartyAction());
            int count = 0;
            StringBuilder sb = new("you control the currently named characters:");
            foreach (uiQuickPlayerCreate player in players)
            {
                if (!Multiplayer.IsYourPhotonId(player.m_PhotonID)) continue;
                if (!FTK_playerGameStartDB.GetDB().IsUnlock((FTK_playerGameStart.ID)player.m_ClassID))
                {
                    window.SetForce(0, "1 of your classes is locked, you must change your classes", "", true);
                    UnregisterDisabledObject.QuickCreate(owner, window);
                    window.Register();
                    return;
                }
                count++;
                sb.Append($" '{player.m_PlayerNameStr} ({player.m_PlayerClass.text})',");
            }
            window.AddAction(new ChoosePartyNamesAction(count));
            window.SetForce(0, $"choose to randomize the classes of the characters you control or give {count} names for them and move to customizing each character", sb.ToString().TrimEnd([',']), true);
            UnregisterDisabledObject.QuickCreate(owner, window);
            window.Register();
        }

        public override string Name => "randomize_party";
        protected override string Description => "randomize the classes of the characters you control. you can choose names afterwards";
        protected override JsonSchema Schema => null;

        protected override void Execute() => SetupParty.NeuroRandomizeParty();

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class ChoosePartyNamesAction(int nameCount = 3) : NeuroAction<List<string>>
    {
        readonly int min = 3;
        readonly int max = 16;

        public override string Name => "choose_party_names";
        protected override string Description => $"pick {nameCount} names for your party members then move to customizing each character before beginning the game";
        protected override JsonSchema Schema => GetSchema();

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
                        MinItems = nameCount,
                        MaxItems = nameCount,
                        UniqueItems = true,
                        Items = new()
                        {
                            Type = JsonSchemaType.String,
                            MinLength = min,
                            MaxLength = max,
                            // Pattern = "^[a-zA-Z]+( [a-zA-Z0-9]+)*$" // start with letters, then optionally 0-1 space with 1+ letters, numbers (allows neuro sama)
                        }
                    }
                }
            };
            return schema;
        }

        protected override void Execute(List<string> parsedData)
        {
            SetupParty.NeuroSetCharacterNames(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out List<string> parsedData)
        {
            parsedData = [];
            JToken token = actionData.Data?.SelectToken("names");
            if (token == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("names"));
            List<string> test = [];
            foreach (JToken name in token)
            {
                if (name.Type != JTokenType.String) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("names"));
                // if (name.Value<string>() is null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("names"));
                test.Add(name.Value<string>());
            }
            List<string> result = token.ToObject<List<string>>();
            if (!result.Count().Equals(nameCount))
            {
                return ExecutionResult.Failure($"choose_party_names action requires {nameCount} names, you sent " + result.Count());
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
    }
}
