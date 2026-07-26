using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class QueryCurrentCOWLocation : NeuroAction
    {
        public override string Name => "query_current_location";
        protected override string Description => "returns the location of the current active overworld character";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld current = GameLogic.Instance.GetCurrentCOW();
            if (current == null)
            {
                Plugin.Logger.LogError("query location failed: no active character");
                Context.Send("query location failed" + NeuroSdkStrings.ModFaultSuffix);
                return;
            }
            string name = current.m_CharacterStats.m_CharacterName;
            HexLand hex = current.GetHexLand();
            MiniHexInfo hexInfo = hex.GetPOI();
            string complete = "";
            if (hexInfo != null)
            {
                if (HexData.IsPoiComplete(hexInfo)) complete = " (completed)";
            }
            Context.Send($"{name} is at {hex.GetPosition()} {hex}. This hex contains ({hexInfo?.GetPOIDisplayValue()} {hexInfo.m_MiniHexType}{complete})");
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            ToggleDisposableActions.DisposeAction(this);
            return ExecutionResult.Success();
        }

        string GetHexData(HexLand hex)
        {
            string data = "";
            MiniHexInfo poi = hex.GetPOI();
            if (poi != null)
            {
                data += $"POI: {poi.GetIDString()}\n";
                if (poi.HasEncounterQuest())
                {
                    QuestLogicBase quest = poi.GetEncounterQuest();
                    data += $"Encounter Quest: {quest.GetLocalizedOneLineDesc()}\n";
                }
                
            }
            return data;
        }
    }
}
