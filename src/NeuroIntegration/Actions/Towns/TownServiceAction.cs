using System.Collections.Generic;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using WebSocketSharp;

namespace Pyran.NeuroFTK
{
    public class TownServiceAction(Dictionary<string, uiFTKButton> _data) : NeuroAction<string>
    {
        public override string Name => "town_service";
        protected override string Description => "choose a town service to purchase";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["service"],
                Properties = new()
                {
                    ["service"] = QJS.Enum(_data.Keys)
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Context.Send($"you have selected the {parsedData} service", true);
            SelectButton.StartCoroutine(_data[parsedData], 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data?.Value<string>("service");
            if (parsedData.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("service"));
            if (!_data.ContainsKey(parsedData)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("service"));
            return ExecutionResult.Success();
        }
    }
}