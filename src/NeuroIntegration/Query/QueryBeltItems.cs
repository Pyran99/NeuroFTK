using System.Collections.Generic;
using System.Text;
using FTKItemName;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryBeltItems(CharacterOverworld cow) : NeuroAction
    {
        public override string Name => "query_belt_items";
        protected override string Description => "see what items from your belt slots you can use right now";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            List<FTK_itembase.ID> items = cow.m_CharacterStats.GetBeltItems();
            StringBuilder sb = new();
            sb.Append("[usable belt items] ");
            foreach (FTK_itembase.ID item in items)
            {
                if ((bool)!FTKItem.Get(item)?.CanUse(cow)) continue;
                sb.AppendLine($"({ItemData.GetItemName(item)}) {ItemData.GetItemDescription(item, true, cow)}");
            }
            if (sb.Length == "[usable belt items] ".Length) sb.Append("none");
            Context.Send(sb.ToString());
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            NeuroActionHandler.UnregisterActions(this);
            return ExecutionResult.Success();
        }
    }
}