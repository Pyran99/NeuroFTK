using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class EndScreenAction(GameEndScreen.EndType endType) : NeuroAction
    {
        public override string Name => "return_to_menu";
        protected override string Description => "finish the adventure and return to the main menu";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            switch (endType)
            {
                case GameEndScreen.EndType.StoneHero:
                    GameEndScreen.SelectButton();
                    break;
                case GameEndScreen.EndType.DungeonRun:
                    FTKClickAnywhere.Instance.OnClick();
                    break;
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}