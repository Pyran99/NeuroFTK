using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class CharacterDecisionAction(string _key, List<VoteButton> _values) : NeuroAction<string>
    {
        public override string Name => $"{_key.Replace(" ", "_")}_decision";
        protected override string Description => $"make the decision for {_key}";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["decision"],
                Properties = new()
                {
                    ["decision"] = QJS.Enum(_values.Select(v => v.m_Option.ToString()))
                },
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            foreach (VoteButton btn in _values)
            {
                if (btn.m_Option.ToString() == parsedData)
                {
                    SelectButton.StartCoroutine(CharacterDecisionButtons.instance, btn, 1.0f);
                    break;
                }
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            Plugin.Logger.LogWarning($"chosen action {actionData.Data}");
            parsedData = actionData.Data.Value<string>("decision");
            foreach (VoteButton btn in _values)
            {
                if (btn.m_Option.ToString() == parsedData)
                {
                    return ExecutionResult.Success();
                }
            }
            return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("decision"));
        }
    }
}