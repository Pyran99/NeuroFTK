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
    }
}