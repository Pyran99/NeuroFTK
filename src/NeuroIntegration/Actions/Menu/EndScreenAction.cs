using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class EndScreenAction : NeuroAction
    {
        public override string Name => "return_to_menu";
        protected override string Description => "finish the adventure and return to the main menu";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            GameEndScreen.SelectButton();
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}