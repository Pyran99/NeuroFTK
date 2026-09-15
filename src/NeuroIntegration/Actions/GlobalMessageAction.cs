using System.Collections.Generic;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class GlobalMessageAction(Dictionary<string, object> _actions) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(uiGlobalMessageHUD _owner, Dictionary<string, object> actions)
        {
            GlobalMessageAction msg = new(actions);
            msg.owner = _owner;
            string ctx = "[Message] " + StringReplace.RemoveStyling(_owner.m_Message.text);
            string query = "use action to continue to the next message";
            if (ctx.Contains("Respawn"))
            {
                query = "choose to revive this character now for a cost, or wait. If another character reaches this location there will not be a cost.";
            }
            ActionWindow window = ActionWindow.Create(_owner.gameObject);
            window.AddAction(msg);
            window.SetForce(2f, query, ctx, true);
            window.SetContext(ctx);
            window.Register();
            return window;
        }

        uiGlobalMessageHUD owner;
        public override string Name => "global_message";
        protected override string Description => "choose action to continue";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["action"],
                Properties = new()
                {
                    ["action"] = QJS.Enum(_actions.Keys),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            if (parsedData == "continue") FTKClickAnywhere.Instance.OnClick();
            else if (parsedData == "yes") SelectButton.StartCoroutine((uiFTKButton)_actions[parsedData]);
            else if (parsedData == "no") SelectButton.StartCoroutine((uiFTKButton)_actions[parsedData]);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string data = actionData.Data?.Value<string>("action");
            if (data.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("action"));
            if (!_actions.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("action"));
            parsedData = data;
            return ExecutionResult.Success();
        }
    }
}