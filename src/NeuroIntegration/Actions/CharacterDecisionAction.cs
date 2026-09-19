using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;
using UnityEngine.UI;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class CharacterDecisionAction(CharacterOverworld cow, string _key, List<VoteButton> _values) : NeuroAction<VoteButton>
    {
        public override string Name => $"{_key.Replace(" ", "_").ToLower()}_decision";
        protected override string Description => $"choose a button with {_key}. focus is an optional property that will only be consumed if the button can be focused.";
        protected override JsonSchema Schema => GetSchema();

        int _focus = 0;

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["button"],
                Properties = new()
                {
                    ["button"] = QJS.Enum(_values.Select(v => v.GetComponentInChildren<Text>().text)),
                    ["focus"] = CharacterData.QuickFocusSchema(cow)
                },
            };
            return schema;
        }

        protected override void Execute(VoteButton parsedData)
        {
            CharacterDecisionButtons.NeuroTryDecisionBtn(parsedData, cow, _focus);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out VoteButton parsedData)
        {
            parsedData = null;
            string result = actionData.Data?.Value<string>("button");
            _focus = actionData.Data?.Value<int>("focus") ?? 0;
            if (result.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("button"));
            foreach (VoteButton btn in _values)
            {
                if (btn.GetComponentInChildren<Text>().text == result)
                {
                    parsedData = btn;
                    return ExecutionResult.Success($"selecting button {parsedData.GetComponentInChildren<Text>().text} with {_key}, using {_focus} focus points");
                }
            }
            return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("button"));
        }
    }
}