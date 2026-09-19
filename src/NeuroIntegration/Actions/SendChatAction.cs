using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class SendChatAction : NeuroAction<string>
    {
        public override string Name => "send_chat_message";
        protected override string Description => "sends a message to the in game chat";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["message"],
                Properties = new()
                {
                    ["message"] = new()
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 3,
                        MaxLength = 150
                    }
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            if (uiStartGame.Instance.m_GameStarted)
            {
                if (GameLogic.Instance.GetCurrentCOW() == null)
                {
                    Context.Send("cannot send game chat msg right now, you are in a loading screen", true);
                    return;
                }
            }
            GameChat.SendChatMsg(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data?.Value<string>("message") ?? "";
            string response = uiChatBox.Instance != null ? "" : "the games chat box is not open right now";
            return ExecutionResult.Success(response);
        }
    }
}