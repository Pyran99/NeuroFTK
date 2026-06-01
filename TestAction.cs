
using System.Collections.Generic;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace NeuroFTK;

public class TestAction : NeuroAction
{
    public override string Name => "test action";

    protected override string Description => "this is a test action";

    protected override JsonSchema Schema => GenerateSchema();

    protected override void Execute()
    {
        Plugin.Logger.LogMessage("executing test action");
    }

    protected override ExecutionResult Validate(ActionJData actionData)
    {
        // Received ws message {"command":"action","data":{"id":"tony_action_1","name":"test action","data":"7"}}
        Plugin.Logger.LogMessage($"validating test action {actionData.Data}");
        int? chosen = (int?)(actionData?.Data);
        if (chosen is not int)
        {
            return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("values"));
        }
        return ExecutionResult.Success();
    }


    JsonSchema GenerateSchema()
    {
        JsonSchema temp = new()
        {
            Type = JsonSchemaType.Integer,
            Required = ["values"],
            Properties = new Dictionary<string, JsonSchema>
            {
                ["values"] = QJS.Enum([0, 1,2,3,4,5])
            }
        };
        return temp;
    }
}