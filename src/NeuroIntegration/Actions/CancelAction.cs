using System;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    /// <summary>
    /// invokes the OnCancelled callback that can be used to Destroy the ActionWindow
    /// </summary>
    /// <param name="_name"></param>
    public class CancelAction(ActionWindow _window, string _description = "") : NeuroAction
    {
        public Action<ActionWindow> OnCancelled { get; set; }

        public override string Name => "cancel_action";
        protected override string Description => _description;
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            OnCancelled?.Invoke(_window);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}