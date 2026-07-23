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
            {VoteButton.VoteOption.Open, "open container, may receive loot"},
            {VoteButton.VoteOption.Collect, "gather the loot"},
            {VoteButton.VoteOption.Equip, "equip the loot"},
            {VoteButton.VoteOption.Use, "use the item right away"},
            {VoteButton.VoteOption.Pass, "throw away the loot"}, // discard
            {VoteButton.VoteOption.Knockdown, "breakdown the object blocking your path"},
            {VoteButton.VoteOption.Disarm, "disarm the trap"},
            {VoteButton.VoteOption.Proceed, "proceed NYI"},
            {VoteButton.VoteOption.Attempt, "attempt NYI"},
            {VoteButton.VoteOption.Ready, "continue to the next room"},
            {VoteButton.VoteOption.Identify, "identify NYI"},
            {VoteButton.VoteOption.Shop, "view the shop"},
            {VoteButton.VoteOption.Destroy, "destroy NYI"},
            {VoteButton.VoteOption.PartyHeal, "partyheal NYI"},
            {VoteButton.VoteOption.DungeonRest, "setup a tinder pouch camp to heal all party members (dont use all of them)"}, // party rest
            {VoteButton.VoteOption.Share, "split the rewards between all involved party members"},
            {VoteButton.VoteOption.AttemptNoRoll, "attemptnoroll NYI"},
        };
    }
}