using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class CowJumpAction : NeuroAction
    {
        public override string Name => "jump";
        protected override string Description => "Jump!";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            if (CameraUtils.IsOnCooldown(Name)) return;
            CharacterOverworld cow = CharacterData.GetActiveCow();
            if (Multiplayer.OtherPlayersAction(cow))
            {
                cow = Multiplayer.GetOwnCow();
                if (cow.GetCombatDummy() == null)
                {
                    Context.Send($"your character is not in combat, they cannot jump right now", true);
                    return;
                }
            }
            Plugin.Instance.StartCoroutine(CameraUtils.CombatJumpCow(cow));
            Context.Send("you have jumped! to the moon!!");
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }
}