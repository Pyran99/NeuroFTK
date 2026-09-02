using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ContinueMessageHudAction : NeuroAction
    {
        public static ActionWindow RegisterAction(GameObject owner)
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new ContinueMessageHudAction());
            window.SetForce(1.5f, "continue to the next message", "a message has appeared on screen", true);
            window.Register();
            return window;
        }

        public override string Name => "continue_message";
        protected override string Description => "continue to the next message";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            //  = new(FTKClickAnywhere.Instance.OnClick, FTKClickAnywhere.Instance.gameObject, 500f);
            FTKClickAnywhere.Instance.StartCoroutine(QuickTimerCallback.WaitRoutine(FTKClickAnywhere.Instance.OnClick, FTKClickAnywhere.Instance.gameObject, 0.5f));
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}