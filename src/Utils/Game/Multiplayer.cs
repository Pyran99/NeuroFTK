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
            if (cow.IsOwner)
            {
                return true;
            }
            if (cow.m_FTKPlayerID.IsLocal())
            {
                return true;
            }
            Plugin.Logger.LogMessage("not your cow");
            return false;
        }

        public static void SendOtherPlayerTurnCtx()
        {
            Context.Send($"another player is taking their turn", true);
        }

        public static CharacterOverworld GetOwnCow()
        {
            Plugin.Logger.LogWarning($"testing:\ncow={GameLogic.Instance.GetCurrentCOW()}\ncombatCow={GameLogic.Instance.GetCurrentCombatCOW()}");
            if (!IsMultiplayer())
            {
                CharacterOverworld cow = GameLogic.Instance.GetCurrentCOW(); // does not change in combat
                if (cow.m_CharacterStats.m_IsInCombat) return GameLogic.Instance.GetCurrentCombatCOW(); // only exists in combat
                return cow;
            }
            foreach (CharacterOverworld cow in FTKHub.Instance.m_CharacterOverworlds)
            {
                if (cow.IsOwner) return cow;
            }
            Plugin.Logger.LogError("could not find own cow");
            return null;
        }
    }
}