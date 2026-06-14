using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK
{
    /// <summary>
    /// return hexid of current COW
    /// </summary>
    public class QueryCurrentCOWLocation : NeuroAction
    {
        public override string Name => "query_current_location";

        protected override string Description => "returns the location of the current active overworld character as a hex id";

        protected override JsonSchema Schema => QJS.ConstNull;

        protected override void Execute()
        {
            
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}