using System.Collections.Generic;
using System.Linq;
using FTKItemName;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class UseBeltItemAction(Dictionary<string, FTK_itembase.ID> items, CharacterOverworld cow) : NeuroAction<FTK_itembase.ID>
    {
        public override string Name => "use_belt_item";
        protected override string Description => "choose an item to use from your belt slots";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["item"],
                Properties = new()
                {
                    ["item"] = QJS.Enum(items.Select(x => x.Key))
                }
            };
            return schema;
        }

        protected override void Execute(FTK_itembase.ID parsedData)
        {
            FTKItem.Get(parsedData)?.OnUse(cow, PlayerInventory.ContainerID.Backpack);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out FTK_itembase.ID parsedData)
        {
            string result = actionData.Data?.Value<string>("item");
            parsedData = items.TryGetValue(result, out parsedData) ? parsedData : FTK_itembase.ID.None;
            if (parsedData == FTK_itembase.ID.None)
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("item"));
            }
            if (!FTKItem.Get(parsedData).CanUse(cow))
            {
                return ExecutionResult.Failure($"cannot use item {parsedData}: {FTKItem.Get(parsedData).GetCannotUseReason(cow)}");
            }
            NeuroActionHandler.UnregisterActions(this);
            return ExecutionResult.Success();
        }
    }
}