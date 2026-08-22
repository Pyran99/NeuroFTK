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
        protected override string Description => "returns the location of the current controlled overworld character, or your character if multiplayer";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld current = CharacterData.GetActiveCow();
            if (current == null)
            {
                Plugin.Logger.LogError("query location failed: no active character?");
                Context.Send("query location failed" + NeuroSdkStrings.ModFaultSuffix);
                return;
            }
            string name = CharacterData.GetCharacterName(current);
            HexLand hex = current.GetHexLand();
            MiniHexInfo hexInfo = hex.GetPOI();
            string complete = "";
            string hexData = "nothing";
            if (hexInfo != null)
            {
                if (!HexData.IsPoiInteractable(hexInfo, current))
                {
                    if (hexInfo.m_Deactivated) complete = " (deactivated)";
                    else if (hexInfo.m_Locked) complete = " (locked)";
                    else complete = " (completed)";
                }
                hexData = $"{hexInfo.GetPOIDisplayValue()}: {hexInfo.m_MiniHexType}{complete}";
            }
            Context.Send($"{name} is at {HexData.GetVec2Pos(hex)} {hex}. This hex contains ({hexData})");
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}
