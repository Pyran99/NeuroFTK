using System.Collections.Generic;

namespace Pyran.NeuroFTK.Utils
{
    public class GameDescriptions
    {
        public static Dictionary<string, string> EncounterDescriptions = new()
        {
            {"Services", "purchase health, focus points, remove debuffs, or remove curses."},
            {"Market", "purchase items."},
            {"Quest Board", "grab a side quest to complete for rewards."},
            {"Leave", "close this menu."},
            {"End Turn", "end this characters turn."},
            {"View Wares", "purchase items."},
            {"Journal", "view this adventurers journal."},
            {"Devote", "become a champion for this sanctums god."},
        };

        public static Dictionary<MiniHexServiceType, string> TownServices = new()
        {
            {MiniHexServiceType.Inn, "replenish some HP and Focus"},
            {MiniHexServiceType.Healer, "replenish all HP and remove debuffs"},
            {MiniHexServiceType.Meditation, "replenish all focus"},
            {MiniHexServiceType.Priest, "remove all curses"},
            {MiniHexServiceType.BoatRepair, "fully repair all adjacent ships"},
            {MiniHexServiceType.BoatReclaim, "exchange an adjacent ship for a ship deed (ship must be fully repaired)"},
        };
    }
}