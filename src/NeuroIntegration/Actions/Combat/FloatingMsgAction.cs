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
        public override string Name => "send_message";
        protected override string Description => "send a random message to appear on the active character for a short time. this action is for fun";
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
                        MaxLength = 30
                        // Pattern = "^[a-zA-Z0-9_ ]+$"
                    }
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            CharacterDummy dummy = CharacterData.GetActiveCow()?.GetCurrentDummy();
            if (dummy == null) return;
            CharacterOverworld cow = dummy.m_CharacterOverworld;
            if (Multiplayer.OtherPlayersAction(cow))
            {
                cow = Multiplayer.GetOwnCow();
                if (cow.GetCombatDummy() == null)
                {
                    Context.Send("your character is not in combat, you cannot send a message", true);
                    return;
                }
            }
            dummy.SpawnHudTextRPC(parsedData);
            Context.Send($"sent msg {parsedData}", true);
            if (uiChatBox.Instance)
            {
                uiChatBox.Instance.AddMessage(UnityEngine.Color.white, cow?.m_CharacterStats.m_CharacterName, parsedData);
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data?.Value<string>("message") ?? "null";
            CharacterDummy dummy = CharacterData.GetActiveCow()?.GetCurrentDummy();
            if (dummy == null) return ExecutionResult.Success("could not send message right now, wait until you are in a battle");
            return ExecutionResult.Success();
        }
    }
}