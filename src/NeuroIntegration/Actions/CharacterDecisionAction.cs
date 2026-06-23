using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK
{
    public class CharacterDecisionAction(string _key, List<string> _values) : NeuroAction<string>
    {
        public override string Name => $"{_key.Replace(" ", "_")}_decision";
        protected override string Description => $"make a decision for {_key}";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["decision"],
                Properties = new()
                {
                    ["decision"] = QJS.Enum(_values)
                },
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage("execute vote btns action");
            foreach (VoteButton btn in CharacterDecisionButtons.voteButtons[_key])
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
            return ExecutionResult.Success();
        }
    }
}