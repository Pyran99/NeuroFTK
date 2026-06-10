using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    public class ConfigueParty : NeuroAction
    {
        public override string Name => "config_party_test";
        protected override string Description => "choose the names of your party";
        protected override JsonSchema Schema => new();


        protected override void Execute()
        {
            SetupParty.NeuroSetCharacterNames(["Player 1", "Player 2", "Player 3"]);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}