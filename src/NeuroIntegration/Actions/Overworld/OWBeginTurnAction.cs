using System.Collections.Generic;
using GridEditor;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class OWBeginTurnAction
    {
        public static ActionWindow CreateWindow(Dictionary<string, FTK_itembase.ID> items)
        {
            CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW();
            ActionWindow window = ActionWindow.Create(cow.gameObject);
            List<INeuroAction> registerActions = [];
            registerActions.Add(new BeginMovementAction());
            if (items.Count > 0)
            {
                registerActions.Add(new UseBeltItemAction(items, cow));
            }
            if (cow.GetHexLand()?.HasPOI() ?? false)
            {
                registerActions.Add(new InteractWithCurrentHex());
            }
            string query = $"your turn for {cow.m_CharacterStats.m_CharacterName} has started. use items or begin your movement choices";
            window.SetForce(0, query, BeginTurns.CtxOverworldTurnBeginStats(cow));
            window.Register();
            return window;
        }
    }

    public class BeginMovementAction : NeuroAction
    {
        public override string Name => "begin_movement";
        protected override string Description => "begins your movement choice";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            OverworldFlow.isFirstAction = false;
            OverworldFlow.StartTracking();
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class ChangeEquipment : NeuroAction
    {
        public override string Name => "change_equipment";
        protected override string Description => "";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}