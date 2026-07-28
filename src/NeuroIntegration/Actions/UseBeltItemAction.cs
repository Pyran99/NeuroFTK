using System.Collections.Generic;
using System.Linq;
using FTKItemName;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class UseBeltItemAction(Dictionary<string, FTK_itembase.ID> items, CharacterOverworld cow, bool remakeOverworld = false) : NeuroAction<FTK_itembase.ID>
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
            if (cow.m_PlayerInventory.GetItemCount(PlayerInventory.ContainerID.Backpack, parsedData) > 0)
            {
                FTKItem.Get(parsedData)?.OnUse(cow, PlayerInventory.ContainerID.Backpack);
            }
            else
            {
                Plugin.Logger.LogError("tried to use an item not on belt " + parsedData);
                Context.Send("the item you tried to use was not in your inventory", true);
            }
            if (remakeOverworld)
            {
                QuickTimerCallback timer = new(OverworldFlow.BeginMovementTurn, cow.gameObject, 0.3f);
                // NeuroActionHandler.UnregisterActions(["use_belt_item"]);
            }
            //=> using items does re initialize battle btns
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
            return ExecutionResult.Success();
        }
    }
}