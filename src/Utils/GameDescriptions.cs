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
            {"Tribute", "activate this statue."},
            {"Enter Party", "begin this dungeon with all party members"},
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

        public static readonly Dictionary<string, string> AlternateLocLookUp = new()
        {
            { "Ice", "Freeze" },
            { "Lightning", "Shock" },
            { "Dazed", "Stun" },
            { "Stunned", "Stun" },
            { "Death", "DeathMark" },
            { "StealItem", "Steal" },
            { "StealGold", "Steal" },
            { "Knockdown", "Bash" },
        };

        public static Dictionary<VoteButton.VoteOption, string> VoteOptionDescriptions = new()
        {
            {VoteButton.VoteOption.Unlocked, "unlocked NYI"},
            {VoteButton.VoteOption.Open, "open NYI"},
            {VoteButton.VoteOption.Collect, "collect NYI"},
            {VoteButton.VoteOption.Equip, "equip NYI"},
            {VoteButton.VoteOption.Use, "use NYI"},
            {VoteButton.VoteOption.Pass, "pass NYI"},
            {VoteButton.VoteOption.Knockdown, "breakdown the object blocking your path"},
            {VoteButton.VoteOption.Disarm, "disarm NYI"},
            {VoteButton.VoteOption.Proceed, "proceed NYI"},
            {VoteButton.VoteOption.Attempt, "attempt NYI"},
            {VoteButton.VoteOption.Ready, "continue to the next room"},
            {VoteButton.VoteOption.Identify, "identify NYI"},
            {VoteButton.VoteOption.Shop, "shop NYI"},
            {VoteButton.VoteOption.Destroy, "destroy NYI"},
            {VoteButton.VoteOption.PartyHeal, "partyheal NYI"},
            {VoteButton.VoteOption.DungeonRest, "dungeonrest NYI"},
            {VoteButton.VoteOption.Share, "share NYI"},
            {VoteButton.VoteOption.AttemptNoRoll, "attemptnoroll NYI"},
        };
    }
}