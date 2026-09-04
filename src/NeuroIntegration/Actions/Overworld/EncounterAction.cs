using System.Collections.Generic;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class EncounterAction(Dictionary<string, uiPoiButton> btns, CharacterOverworld cow) : NeuroAction<object[]>
    {
        public static ActionWindow CreateWindow(MonoBehaviour _instance, Dictionary<string, uiPoiButton> _btns, string _context = "")
        {
            CharacterOverworld cow = CharacterData.GetActiveCow();
            ActionWindow window = ActionWindow.Create(_instance.gameObject);
            window.AddAction(new EncounterAction(_btns, cow));
            window.SetForce(3, "choose an action for this encounter", $"{CharacterData.GetDataFor(cow)} {StringMessages.FocusDetails}", true);
            if (_context != "") window.SetContext(_context);
            window.Register();
            return window;
        }

        public override string Name => "encounter";
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
                    [prop] = QJS.Enum(btns.Keys),
                    ["focus"] = CharacterData.QuickFocusSchema(cow)
                }
            };
            return schema;
        }

        protected override void Execute(object[] parsedData)
        {
            foreach (KeyValuePair<string, uiPoiButton> btn in btns)
            {
                if (btn.Key == (string)parsedData[0])
                {
                    int slots = RollSlotOutcomes.GetOutcomes(cow, Encounters.GetSlotId(btn.Value.m_ButtonInfo.m_ButtonType, cow)).Count;
                    RollSystem.chosenBtn = btn.Value;
                    if (CharacterData.CanFocusAction(cow.m_CharacterStats, slots, (int)parsedData[1]))
                    {
                        SelectButton.StartCoroutineWithFocus(btn.Value, (int)parsedData[1], cow.m_CharacterStats);
                    }
                    else SelectButton.StartCoroutine(btn.Value, 1.0f);
                    return;
                }
            }
            Plugin.Logger.LogError("failed to select button " + parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[2];
            string result = actionData.Data?.Value<string>(prop);
            int focus = actionData.Data?.Value<int>("focus") ?? 0;
            if (btns.Count == 0) return ExecutionResult.Success();
            if (result.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format(prop));
            if (!btns.ContainsKey(result)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format(prop));
            // if (!btns.Any(b => b.m_ButtonText.text == result)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format(prop));
            parsedData[0] = result;
            parsedData[1] = focus;
            return ExecutionResult.Success();
        }
    }
}