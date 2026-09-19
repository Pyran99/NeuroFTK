using System.Collections.Generic;
using System.Text;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryBeltItems : NeuroAction<string>
    {
        readonly Dictionary<string, CharacterOverworld> cows = CharacterData.GetCows(true);

        public override string Name => "query_belt_items";
        protected override string Description => "see what quick use items are on a characters belt";
        protected override JsonSchema Schema => GetSchema();
        readonly string prop = "character";

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = [prop],
                Properties = new()
                {
                    [prop] = QJS.Enum(cows.Keys),
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            CharacterOverworld cow = cows.TryGetValue(parsedData, out cow) ? cow : null;
            if (cow == null)
            {
                Plugin.Logger.LogError("invalid belt query");
                Context.Send("invalid belt query", true);
                return;
            }
            string title = $"[{CharacterData.GetCharacterName(cow)} usable belt items] ";
            StringBuilder sb = new(title);
            string blacklist;
            foreach (FTK_itembase.ID item in cow.m_CharacterStats.GetBeltItems())
            {
                blacklist = "";
                if (ItemData.IsBlacklistItem(item)) blacklist = "(this item is not implemented for you yet)";
                sb.AppendLine($"({ItemData.GetItemName(item)}) {ItemData.GetItemDescription(item, cow, true, true)}{blacklist}");
            }
            if (sb.Length == title.Length) sb.Append($"there are no items on {CharacterData.GetCharacterName(cow)}'s belt");
            Context.Send(sb.ToString());
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data?.Value<string>(prop) ?? "";
            if (parsedData.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(prop));
            if (!cows.ContainsKey(parsedData)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format(prop));
            return ExecutionResult.Success();
        }
    }
}