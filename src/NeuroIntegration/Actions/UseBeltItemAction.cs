using System.Collections.Generic;
using System.Linq;
using FTKItemName;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class UseBeltItemAction(Dictionary<string, FTK_itembase.ID> items, CharacterOverworld cow, bool remakeOverworld = false, bool remakeCombat = false) : NeuroAction<FTK_itembase.ID>
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
            if (remakeOverworld)
            {
                QuickTimerCallback timer = new(OverworldFlow.BeginMovementTurn, cow.gameObject, 0.3f);
            }
            else if (remakeCombat)
            {
                QuickTimerCallback timer = new(() => Battle.CreateActionWindow(Battle.StanceBtnInstance, Battle.m_Proficiencies), Battle.StanceBtnInstance.gameObject, 0.3f);
            }
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