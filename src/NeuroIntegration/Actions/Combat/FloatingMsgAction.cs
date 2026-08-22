using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    // send a message from the combat floating text
    public class FloatingMsgAction : NeuroAction<string>
    {
        public override string Name => "send_msg";
        protected override string Description => "send a random message to appear on the active character. this action is for chat engagement";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["action"],
                Properties = new()
                {
                    ["action"] = new()
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 3,
                        MaxLength = 30,
                        Pattern = "^[a-zA-Z0-9_ ]+$"
                    }
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            CharacterData.GetActiveCow().GetCurrentDummy()?.SpawnHudTextRPC(parsedData);
            if (uiChatBox.Instance)
            {
                uiChatBox.Instance.AddMessage(UnityEngine.Color.white, CharacterData.GetActiveCow()?.m_CharacterStats.m_CharacterName, parsedData);
                Context.Send($"sent msg {parsedData}", true);
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data?.Value<string>("action") ?? "null";
            return ExecutionResult.Success();
        }
    }
}