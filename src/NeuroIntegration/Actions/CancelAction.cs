using System;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK
{
    /// <summary>
    /// invokes the OnCancelled callback that can be used to unregister an action
    /// </summary>
    /// <param name="_name"></param>
    public class CancelAction(string _name, string extraDescription = "") : NeuroAction
    {
        public Action<NeuroAction> OnCancelled { get; set; }

        public override string Name => "cancel_action";

        protected override string Description => $"Cancel the {_name} action. {extraDescription}";

        protected override JsonSchema Schema => new();

        protected override void Execute()
        {
            OnCancelled?.Invoke(this);
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}