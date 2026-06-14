using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using UnityEngine;

namespace Pyran.NeuroFTK
{
    public class ContinueMessageHudAction : NeuroAction
    {
        public static ActionWindow RegisterAction(GameObject owner)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ContinueMessageHudAction());
            window.SetForce(1, "continue to the next message", "a message has appeared on screen", true);
            window.Register();
            return window;
        }

        public override string Name => "continue_message";
        protected override string Description => "continue to the next message";
        protected override JsonSchema Schema => QJS.ConstNull;

        protected override void Execute()
        {
            Plugin.Logger.LogMessage("message hud continue");
            FTKClickAnywhere.Instance.OnClick();
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}