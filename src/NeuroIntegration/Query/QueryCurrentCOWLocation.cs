using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK
{
    public class QueryCurrentCOWLocation : NeuroAction
    {
        public override string Name => "query_current_location";
        protected override string Description => "returns the location of the current active overworld character";
        protected override JsonSchema Schema => new();

        protected override void Execute()
        {
            CharacterOverworld current = GameLogic.Instance.GetCurrentCOW();
            string name = current.m_CharacterStats.m_CharacterName;
            if (current.IsInDungeon())
            {
                Context.Send($"{name} is in a duungeon");
                return;
            }
            HexLand hex = current.GetHexLand();
            Context.Send($"{name} is at {hex.GetPosition()} - {hex}");
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}
