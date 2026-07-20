using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.Utils
{
    public class Multiplayer
    {
        public static bool IsMultiplayer()
        {
            return GameLogic.Instance.IsMultiplayer() || GameLogic.Instance.IsLocalMultiplayer();
        }

        public static bool IsOwnerTurn(CharacterOverworld cow)
        {
            if (cow.IsOwner)
            {
                Plugin.Logger.LogWarning("owner cow");
                return true;
            }
            if (cow.m_FTKPlayerID.IsLocal())
            {
                Plugin.Logger.LogWarning("local cow");
                return true;
            }
            Plugin.Logger.LogWarning("not your cow");
            return false;
        }

        public static void SendOtherPlayerTurnCtx()
        {
            Context.Send($"another player is taking their turn", true);
        }

        public static CharacterOverworld GetOwnCow()
        {
            foreach (CharacterOverworld cow in FTKHub.Instance.m_CharacterOverworlds)
            {
                if (cow.IsOwner) return cow;
            }
            Plugin.Logger.LogError("could not find own cow");
            return null;
        }
    }
}