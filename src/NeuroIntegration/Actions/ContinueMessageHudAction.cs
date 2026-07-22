using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ContinueMessageHudAction : NeuroAction
    {
        public static ActionWindow RegisterAction(GameObject owner)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ContinueMessageHudAction());
            window.SetForce(0, "continue to the next message", "a message has appeared on screen", true);
            window.Register();
            return window;
        }

        public override string Name => "continue_message";
        protected override string Description => "continue to the next message";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            FTKClickAnywhere.Instance.OnClick();
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}