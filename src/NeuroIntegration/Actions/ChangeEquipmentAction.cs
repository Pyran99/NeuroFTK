using System;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class ChangeEquipmentAction(Dictionary<PlayerInventory.ContainerID, Dictionary<string, string>> _items) : NeuroAction<string>
    {
        public override string Name => "equip_items";
        protected override string Description => "equip an item from your inventory";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["item"],
                Properties = GetAvailableProperties(FTK_itembase.ObjectType.helmet, CharacterData.GetActiveCow())
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Properties = new()
                {
                    ["item"] = QJS.Enum(["1","2"]),
                    ["equip"] = QJS.Type(JsonSchemaType.Boolean),
                    ["force"] = QJS.Type(JsonSchemaType.Boolean)
                }
            };
            
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            Plugin.Logger.LogWarning(actionData.Data?.ToString());
            return ExecutionResult.Success();
        }

        Dictionary<string, JsonSchema> GetAvailableProperties(FTK_itembase.ObjectType type, CharacterOverworld cow)
        {
            FTK_itembase.ObjectType[] types =
            {
                FTK_itembase.ObjectType.weapon,
                FTK_itembase.ObjectType.shield,
                FTK_itembase.ObjectType.armor,
                FTK_itembase.ObjectType.helmet,
                FTK_itembase.ObjectType.trinket,
                FTK_itembase.ObjectType.boots,
                FTK_itembase.ObjectType.necklace,
            };
            Dictionary<string, JsonSchema> result = [];
            foreach (FTK_itembase.ObjectType t in types)
            {
                if (t == FTK_itembase.ObjectType.weapon || t == FTK_itembase.ObjectType.shield)
                {
                    //TODO
                    continue;
                }
                else
                {
                    if (!CharacterData.IsEquipmentEmpty(CharacterData.GetContainerForItem(t), cow)) continue;
                }
                List<FTK_itembase.ID> items2 = cow.m_CharacterStats.GetPackItems(t, true);
                if (items2.Count > 0)
                {
                    result[t.ToString()] = QJS.Enum(items2.Select(ItemData.GetItemName).ToList());
                }
            }
            return result;
        }

        static bool Test2()
        {
            return false;
        }
    }
}