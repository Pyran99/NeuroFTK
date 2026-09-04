using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using UnityEngine.UI;
using WebSocketSharp;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class CharacterDecisionAction(CharacterOverworld cow, string _key, List<VoteButton> _values) : NeuroAction<VoteButton>
    {
        public override string Name => $"{_key.Replace(" ", "_").ToLower()}_decision";
        protected override string Description => $"choose a button with {_key}";
        protected override JsonSchema Schema => GetSchema();

        int _focus = 0;

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["button"],
                Properties = new()
                {
                    ["button"] = QJS.Enum(_values.Select(v => v.GetComponentInChildren<Text>().text)),
                    ["focus"] = CharacterData.QuickFocusSchema(cow)
                },
            };
            return schema;
        }

        protected override void Execute(VoteButton parsedData)
        {
            Context.Send($"selecting button {parsedData.GetComponentInChildren<Text>().text} with {_key}, using {_focus} focus points", true);
            if (_focus <= 0)
            {
                SelectButton.StartCoroutine(parsedData, 1.0f);
                return;
            }
            FTK_slotOutput.ID id = FTK_slotOutput.ID.None;
            if (EncounterSession.Instance.m_ActiveDiorama is DioramaDungeon dioramaDungeon)
            {
                switch (parsedData.m_Option)
                {
                    case VoteButton.VoteOption.Knockdown:
                        id = dioramaDungeon.m_DoorToBash.GetComponent<DungeonDoor>().GetDoorBashOutput(parsedData.m_Hud.m_Cow);
                        break;
                    case VoteButton.VoteOption.Disarm:
                        id = dioramaDungeon.m_ActiveTrap.GetDisarmOutput(parsedData.m_Hud.m_Cow);
                        break;
                    case VoteButton.VoteOption.Proceed:
                        id = dioramaDungeon.m_ActiveTrap.GetProceedOutput(parsedData.m_Hud.m_Cow);
                        break;
                    case VoteButton.VoteOption.Attempt:
                        id = dioramaDungeon.m_DungeonEncounter.m_EncounterObject.GetDBEntry().m_SlotRoll;
                        break;
                }
            }
            if (id == FTK_slotOutput.ID.None)
            {
                SelectButton.StartCoroutine(parsedData, 1.0f);
                return;
            }
            FTK_slotOutput entry = FTK_slotOutputDB.GetDB().GetEntry(id);
            if (!entry.m_CanFocus)
            {
                SelectButton.StartCoroutine(parsedData, 1.0f);
                return;
            }
            if (CharacterData.CanFocusAction(cow.m_CharacterStats, entry.m_SlotAmount, _focus))
            {
                SelectButton.StartCoroutineWithFocus(parsedData, _focus, cow.m_CharacterStats);
            }
            else SelectButton.StartCoroutine(parsedData, 1.0f);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out VoteButton parsedData)
        {
            parsedData = null;
            string result = actionData.Data?.Value<string>("button");
            _focus = actionData.Data?.Value<int>("focus") ?? 0;
            if (result.IsNullOrEmpty()) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("button"));
            foreach (VoteButton btn in _values)
            {
                if (btn.GetComponentInChildren<Text>().text == result)
                {
                    parsedData = btn;
                    return ExecutionResult.Success();
                }
            }
            return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("button"));
        }
    }
}