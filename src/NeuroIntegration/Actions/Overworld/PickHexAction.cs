using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class PickHexAction(Dictionary<string, HexLand> _hexPositions) : NeuroAction<HexLand>
    {
        public static ActionWindow CreateWindow(CharacterOverworld _cow, string ctx, Dictionary<string, HexLand> hexPositions)
        {
            ActionWindow window = ActionWindow.Create(_cow.gameObject);
            window.AddAction(new PickHexAction(hexPositions));
            window.SetContext(ctx);
            window.SetForce(0, "choose a hex to use the item on", "", true);
            window.Register();
            return window;
        }

        public override string Name => "pick_hex";
        protected override string Description => "select a hex position to use the item on";
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
                return;
            }
            HexPick.PickHex(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out HexLand parsedData)
        {
            parsedData = null;
            string data = actionData.Data?.Value<string>("hex");
            if (data == null || data == string.Empty) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("hex"));
            if (!_hexPositions.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("hex"));
            parsedData = _hexPositions[data];
            return ExecutionResult.Success();
        }
    }
}