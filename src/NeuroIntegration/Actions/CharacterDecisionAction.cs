using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class CharacterDecisionAction(string _key, List<VoteButton> _values) : NeuroAction<VoteButton>
    {
        public override string Name => $"{_key.Replace(" ", "_")}_decision";
        protected override string Description => $"choose a button with {_key}";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["button"],
                Properties = new()
                {
                    ["button"] = QJS.Enum(_values.Select(v => v.m_Option.ToString()))
                },
            };
            return schema;
        }

        protected override void Execute(VoteButton parsedData)
        {
            Context.Send($"selecting button {parsedData.m_Option} with {_key}", true);
            SelectButton.StartCoroutine(parsedData, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out VoteButton parsedData)
        {
            parsedData = null;
            string result = actionData.Data.Value<string>("button");
            foreach (VoteButton btn in _values)
            {
                if (btn.m_Option.ToString() == result)
                {
                    parsedData = btn;
                    return ExecutionResult.Success();
                }
            }
            return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("button"));
        }
    }
}