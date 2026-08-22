using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.Utils
{
    public class Multiplayer
    {
        public static bool OtherPlayersAction(CharacterOverworld cow)
        {
            if (!IsMultiplayer()) return false;
            if (!IsYourCow(cow)) return true;
            return false;
        }

        public static bool IsMultiplayer()
        {
            return GameLogic.Instance.IsMultiplayer() || GameLogic.Instance.IsLocalMultiplayer();
        }

        public static bool IsYourCow(CharacterOverworld cow)
        {
            if (cow.IsOwner) return true;
            if (cow.m_FTKPlayerID.IsLocal()) return true;
            return false;
        }

        public static void SendOtherPlayerTurnCtx()
        {
            Context.Send($"another player is taking their turn", true);
        }

        /// <returns>IsOwner Cow or active if not multiplayer</returns>
        public static CharacterOverworld GetOwnCow()
        {
            if (!IsMultiplayer())
            {
                return CharacterData.GetActiveCow();
            }
            foreach (CharacterOverworld _cow in FTKHub.Instance.m_CharacterOverworlds)
            {
                if (IsYourCow(_cow)) return _cow;
            }
            Plugin.Logger.LogError("could not find own cow");
            return null;
        }
    }
}