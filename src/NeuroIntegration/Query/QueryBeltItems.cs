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
        protected override string Description => "see what quick use items are on the belt of the current controlled character";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld cow = CharacterData.GetNeuroCow();
            string title = $"[{CharacterData.GetCharacterName(cow)} usable belt items] ";
            StringBuilder sb = new(title);
            string blacklist;
            foreach (FTK_itembase.ID item in cow.m_CharacterStats.GetBeltItems())
            {
                blacklist = "";
                if (ItemData.IsBlacklistItem(item))
                {
                    blacklist = "(this item is not implemented for you yet)";
                }
                sb.AppendLine($"({ItemData.GetItemName(item)}) {ItemData.GetItemDescription(item, true, cow)}{blacklist}");
            }
            if (sb.Length == title.Length) sb.Append($"there are no items on {CharacterData.GetCharacterName(cow)}'s belt");
            Context.Send(sb.ToString());
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}