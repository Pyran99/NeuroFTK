using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    public class RewardMenuAction(string _menuName, Dictionary<string, uiChooseRewardButton> _buttons) : NeuroAction<string>
    {

        public static ActionWindow RegisterActions(MonoBehaviour owner, Dictionary<string, uiChooseRewardButton> _buttons, string menuName)
        {
            menuName = Regex.Replace(menuName, " ", "_").ToLower();
            ActionWindow window = ActionWindow.Create(owner.gameObject);
            window.AddAction(new RewardMenuAction(menuName, _buttons));
            if (ChooseRewardMenu.teamState != string.Empty) window.SetContext(ChooseRewardMenu.teamState);
            ChooseRewardMenu.teamState = string.Empty;
            window.SetForce(0, "select an option from this menu", "");
            window.Register();
            return window;
        }

        public override string Name => _menuName;
        protected override string Description => "choose a reward or select a character";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["action"],
                Properties = new()
                {
                    ["action"] = QJS.Enum(_buttons.Keys)
                }
            };
            return schema;
        }

        protected override void Execute(string parsedData)
        {
            uiChooseRewardButton btn = _buttons[parsedData];
            SelectButton.StartCoroutine(btn, 0.5f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out string parsedData)
        {
            parsedData = "";
            string data = actionData.Data?.Value<string>("action");
            if (data.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("action"));
            if (!_buttons.ContainsKey(data)) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("action"));
            parsedData = data;
            return ExecutionResult.Success();
        }
    }
}