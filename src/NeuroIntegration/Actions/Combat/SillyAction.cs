using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    // just a test for neuro to send game message. there is also a chat window
    public class SillyAction : NeuroAction<string>
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
                        Pattern = "^[a-zA-Z0-9_]+$"
                    }
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            GameLogic.Instance.GetCurrentCombatCOW().GetCurrentDummy()?.SpawnHudTextRPC(parsedData);
            if (uiChatBox.Instance)
            {
                uiChatBox.Instance.AddMessage(UnityEngine.Color.white, GameLogic.Instance.GetCurrentCombatCOW()?.m_CharacterStats.m_CharacterName, parsedData);
                Context.Send($"sent msg {parsedData}", true);
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data.Value<string>("action") ?? "null";
            return ExecutionResult.Success();
        }
    }
}