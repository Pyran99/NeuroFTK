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
        public static ActionWindow CreateWindow(MonoBehaviour _instance, List<uiPoiButton> _btns, string _context = "")
        {
            ActionWindow window = ActionWindow.Create(_instance.gameObject);
            window.AddAction(new EncounterAction(_instance, _btns));
            window.SetForce(3, "choose an action for this encounter", "you encountered something in the overworld and a menu appeared", true);
            if (_context != "") window.SetContext(_context);
            window.Register();
            return window;
        }

        public override string Name => "encounter";
        protected override string Description => "choose what to do at this encounter";
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
            foreach (uiPoiButton btn in btns)
            {
                if (btn.m_ButtonText.text == parsedData)
                {
                    SelectButton.StartCoroutine(btn, 1.0f);
                    return;
                }
            }
            Plugin.Logger.LogError("failed to select button " + parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string result = actionData.Data.Value<string>("action");
            if (btns.Count == 0) return ExecutionResult.Success();
            if (result == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("action"));
            if (!btns.Any(b => b.m_ButtonText.text == result)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("action"));
            parsedData = result;
            return ExecutionResult.Success();
        }
    }
}