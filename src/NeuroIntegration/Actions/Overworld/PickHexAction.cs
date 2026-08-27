using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PickHexAction(Dictionary<string, HexLand> _hexPositions, FTK_itembase.ID item) : NeuroAction<HexLand>
    {
        public static ActionWindow CreateWindow(CharacterOverworld _cow, string ctx, Dictionary<string, HexLand> hexPositions, FTK_itembase.ID _item)
        {
            ActionWindow window = ActionWindow.Create(_cow.gameObject);
            window.AddAction(new PickHexAction(hexPositions, _item));
            window.SetContext(ctx);
            window.SetForce(0, "choose a hex to use the item on", "", true);
            window.Register();
            return window;
        }

        public override string Name => "pick_hex";
        protected override string Description => $"select a hex position to use {ItemData.GetItemName(item)} on";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["hex"],
                Properties = new()
                {
                    ["hex"] = QJS.Enum(_hexPositions.Select(x => x.Key).ToList())
                }
            };
            return schema;
        }

        protected override void Execute(HexLand parsedData)
        {
            if (parsedData == null)
            {
                Plugin.Logger.LogError($"did not find {parsedData} in tiles");
                parsedData = CharacterData.GetActiveCow().GetHexLand();
            }
            HexPick.PickHex(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out HexLand parsedData)
        {
            parsedData = null;
            string data = actionData.Data?.Value<string>("hex");
            if (data.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("hex"));
            if (!_hexPositions.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("hex"));
            if (!Movement.Instance.m_PickHexClient.PickHexValidCallback(_hexPositions[data]))
            {
                return ExecutionResult.Failure($"your choice {data} was invalid, select a different option");
            }
            parsedData = _hexPositions[data];
            return ExecutionResult.Success();
        }
    }
}