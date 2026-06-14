using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

namespace Pyran.NeuroFTK
{
    /// <summary>
    /// return hexid of current COW
    /// </summary>
    public class QueryCurrentCOWLocation : NeuroAction
    {
        public override string Name => "query_current_location";

        protected override string Description => "returns the location of the current active overworld character as a hex id";

        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            CharacterOverworld current = GameLogic.Instance.GetCurrentCOW();
            Plugin.Logger.LogMessage(current);
            string name = current.m_CharacterStats.m_CharacterName;
            Plugin.Logger.LogMessage(name);
            HexLand hex = current.GetHexLand();
            Plugin.Logger.LogMessage(hex);
            HexLandID id = hex.GetHexLandID();
            Plugin.Logger.LogMessage($"{id.m_BigIndex} - {id.m_SmallIndex}");
            if (current.IsInDungeon())
            {
                Plugin.Logger.LogMessage("COW in dungeon");
                return;
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}

// [Message:Neuro For the King] Player 1 (CharacterOverworld)
// [Message:Neuro For the King] in pr
// [Message:Neuro For the King] ForestVillage01 (HexLand)
// [Message:Neuro For the King] 25 - 18