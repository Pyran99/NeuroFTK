using System.Text;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryBeltItems() : NeuroAction
    {
        public override string Name => "query_belt_items";
        protected override string Description => "see what items from your belt slots you can use right now";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld cow = CharacterData.GetNeuroCow();
            string title = "[usable belt items] ";
            StringBuilder sb = new(title);
            foreach (FTK_itembase.ID item in ItemData.GetUsableBeltItems(cow))
            {
                sb.AppendLine($"({ItemData.GetItemName(item)}) {ItemData.GetItemDescription(item, true, cow)}");
            }
            if (sb.Length == title.Length) sb.Append("there are no items you can use right now");
            Context.Send(sb.ToString());
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            ToggleDisposableActions.DisposeAction(this);
            return ExecutionResult.Success();
        }
    }
}