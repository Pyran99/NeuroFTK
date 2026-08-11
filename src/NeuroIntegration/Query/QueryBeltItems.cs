using System.Collections.Generic;
using System.Text;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryBeltItems : NeuroAction<string>
    {
        readonly List<string> names = Names();

        public override string Name => "query_belt_items";
        protected override string Description => "see what quick use items are on a characters belt. leave empty to choose the current character. if playing in multiplayer this will always choose your character";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Properties = new()
                {
                    ["character"] = QJS.Enum(names),
                }
            };
            return schema;
        }

        static List<string> Names()
        {
            List<string> result = [];
            foreach (CharacterOverworld cow in FTKHub.Instance.m_CharacterOverworlds)
            {
                result.Add(CharacterData.GetCharacterName(cow));
            }
            return result;
        }

        protected override void Execute(string parsedData)
        {
            CharacterOverworld cow;
            if (Multiplayer.IsMultiplayer()) cow = CharacterData.GetNeuroCow();
            else if (parsedData == string.Empty || !names.Contains(parsedData)) cow = CharacterData.GetNeuroCow();
            else cow = FTKHub.Instance.m_CharacterOverworlds.Find(cow => CharacterData.GetCharacterName(cow) == parsedData);
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

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = actionData.Data?.Value<string>("character") ?? "";
            return ExecutionResult.Success();
        }
    }
}