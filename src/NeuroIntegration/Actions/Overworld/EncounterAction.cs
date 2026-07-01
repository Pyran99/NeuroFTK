using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class EncounterAction(MonoBehaviour instance, List<uiPoiButton> btns) : NeuroAction<string>
    {
        public override string Name => "encounter";
        protected override string Description => "encounter_desc";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["action"],
                Properties = new()
                {
                    ["action"] = QJS.Enum(btns.Select(b => b.m_ButtonText.text).ToList())
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            Plugin.Logger.LogMessage("execute " + parsedData);
            foreach (uiPoiButton btn in btns)
            {
                if (btn.m_ButtonText.text == parsedData)
                {
                    SelectButton.StartCoroutine(instance, btn, 1.0f);
                    return;
                }
            }
            Plugin.Logger.LogError("failed to select button " + parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data.Value<string>("action");
            if (result == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("action"));
            if (!btns.Any(b => b.m_ButtonText.text == result)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("action"));
            parsedData = result;
            return ExecutionResult.Success();
        }
    }
}