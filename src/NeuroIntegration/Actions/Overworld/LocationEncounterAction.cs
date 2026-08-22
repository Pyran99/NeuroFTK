using System.Collections.Generic;
using System.Linq;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class LocationEncounterAction(Dictionary<string, uiLocationMenuEntry> _buttons) : NeuroAction<string>
    {
        public static ActionWindow RegisterAction(GameObject owner, Dictionary<string, uiLocationMenuEntry> _buttons, string _context = "")
        {
            ActionWindow window = ActionWindow.Create(owner);
            window.AddAction(new LocationEncounterAction(_buttons));
            window.SetForce(3, "choose an action for this location encounter", "you encountered something in the overworld and a menu appeared");
            if (_context != "") window.SetContext(_context);
            window.Register();
            return window;
        }

        public override string Name => "location_encounter";
        protected override string Description => "choose what to do at this encounter";
        protected override JsonSchema Schema => GetSchema();
        readonly string prop = "action";

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = [prop],
                Properties = new()
                {
                    [prop] = QJS.Enum(_buttons.Keys.ToList())
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            if (!uiLocationMenuDisplay.Instance.IsShowing())
            {
                Plugin.Logger.LogError("location menu is not showing");
                return;
            }
            uiLocationMenuEntry entry = _buttons[parsedData];
            Plugin.Logger.LogWarning("entry method call = " + entry.m_MethodInfo.ToString()); // Void OnDevote
            // if (parsedData == "Enter Party") // scuff
            // {
            //     GameStates.mode = uiGameTrackerHUD.GameTrackerMode.Dungeon;
            // }
            SelectButton.StartCoroutine(entry.GetComponent<ServiceButton>());
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string data = actionData.Data?.Value<string>(prop) ?? "";
            if (data.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(prop));
            if (!_buttons.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format(prop));
            parsedData = data;
            return ExecutionResult.Success();
        }
    }
}