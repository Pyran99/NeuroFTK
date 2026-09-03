using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ChangeEquipmentAction(Dictionary<PlayerInventory.ContainerID, Dictionary<string, FTK_itembase.ID>> _items, CharacterOverworld _cow) : NeuroAction<List<FTK_itembase.ID>>
    {
        public override string Name => "equip_items";
        protected override string Description => $"equip items from {CharacterData.GetCharacterName(_cow)} inventory. You can choose any number of types. if you select a left hand and right hand item at the same time, if the right hand is 2 handed it will remove the left hand item selected or already equipped";
        protected override JsonSchema Schema => GetSchema();

        private readonly Dictionary<string, FTK_itembase.ID> props = [];

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Properties = GetAvailableProperties(_items)
            };
            return schema;
        }

        protected override ExecutionResult Validate(ActionJData actionData, out List<FTK_itembase.ID> parsedData)
        {
            parsedData = [];
            Plugin.Logger.LogMessage("change equipment data " + actionData.Data?.ToString());
            string chosen;
            foreach (JToken token in actionData.Data) // "Head": "Old Leather Helm"
            {
                foreach (JToken child in token.Children())
                {
                    chosen = child.Value<string>()?.ToLower() ?? ""; // Old Leather Helm
                    if (props.TryGetValue(chosen, out FTK_itembase.ID itemID))
                    {
                        parsedData.Add(itemID);
                    }
                }
            }
            return ExecutionResult.Success();
        }

        protected override void Execute(List<FTK_itembase.ID> parsedData)
        {
            _cow.StartCoroutine(EquipmentManager.EquipItemsRoutine(parsedData, _cow));
        }

        Dictionary<string, JsonSchema> GetAvailableProperties(Dictionary<PlayerInventory.ContainerID, Dictionary<string, FTK_itembase.ID>> items)
        {
            Dictionary<string, JsonSchema> result = [];
            foreach (PlayerInventory.ContainerID container in items.Keys)
            {
                result.Add(container.ToString(), QJS.Enum(items[container].Keys.ToList()));
                foreach (KeyValuePair<string, FTK_itembase.ID> kvp in items[container])
                {
                    props.Add(kvp.Key, kvp.Value);
                }
            }
            if (result.Count == 0) Plugin.Logger.LogError("no equipment at action");
            return result;
        }
    }
}