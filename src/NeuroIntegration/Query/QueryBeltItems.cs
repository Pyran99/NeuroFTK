using System.Collections.Generic;
using System.Linq;
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
        protected override string Description => "see whats items from your belt slots you can use right now";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            List<FTK_itembase.ID> items = cow.m_CharacterStats.GetBeltItems();
            Dictionary<string, string> data = [];
            foreach (FTK_itembase.ID item in items)
            {
                if ((bool)!FTKItem.Get(item)?.CanUse(cow)) continue;
                data.Add(ItemData.GetItemName(item), ItemData.GetItemDescription(item, true, cow));
            }
            Context.Send($"[quick use items] {string.Join("\n", [.. data.Select(x => $"({x.Key}) {x.Value}")])}");
            Plugin.devConsole?.PrintToConsole($"[quick use items] {string.Join(", ", [.. data.Select(x => $"({x.Key}) {x.Value}")])}");
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}