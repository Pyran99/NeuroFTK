using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryStatusEffects() : NeuroAction
    {
        public override string Name => "query_status_effects";
        protected override string Description => "get a list of status effects, curses and immunities applied to the currently controlled character (or your character if in multiplayer)";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld cow = CharacterData.GetActiveCow();
            Context.Send(CharacterData.GetAllStatusEffects(cow));
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}