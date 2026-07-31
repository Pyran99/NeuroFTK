using System.Collections.Generic;
using Google2u;

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
            {"Rest", "recover HP and end turn."},
            {"Meditate", "recover Focus and end turn."},
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
            {VoteButton.VoteOption.Unlocked, "use lockpicks to open the object"},
            {VoteButton.VoteOption.Open, "open container, may receive loot"},
            {VoteButton.VoteOption.Collect, "gather the loot"},
            {VoteButton.VoteOption.Equip, "equip the loot"},
            {VoteButton.VoteOption.Use, "use the item right away"},
            {VoteButton.VoteOption.Pass, "throw away the loot"}, // discard
            {VoteButton.VoteOption.Knockdown, "breakdown the object blocking your path"},
            {VoteButton.VoteOption.Disarm, "disarm the trap"},
            {VoteButton.VoteOption.Proceed, "attempt to pass the encounter"},
            {VoteButton.VoteOption.Attempt, "attempt to disable the device"},
            {VoteButton.VoteOption.Ready, "continue to the next room"},
            {VoteButton.VoteOption.Identify, "identify if the chest is a mimic"},
            {VoteButton.VoteOption.Shop, "view the shop"},
            {VoteButton.VoteOption.Destroy, "destroy NYI"},
            {VoteButton.VoteOption.PartyHeal, "partyheal NYI"},
            {VoteButton.VoteOption.DungeonRest, "setup a tinder pouch camp to heal all party members (dont use all of them)"}, // party rest
            {VoteButton.VoteOption.Share, "split the rewards between all involved party members"},
            {VoteButton.VoteOption.AttemptNoRoll, "attemptnoroll NYI"},
        };

        public static string GetEncounterBtnFlavor(SubPanelBaseBase.ButtonID id)
        {
            return id switch
            {
                SubPanelBaseBase.ButtonID.Fight => FTKHub.Localized<TextInfo>("STR_CombatFightSelect"),
                SubPanelBaseBase.ButtonID.Ambush => FTKHub.Localized<TextInfo>("STR_CombatAmbushSelect") + "(based on awareness stat)",
                SubPanelBaseBase.ButtonID.Defend => FTKHub.Localized<TextInfo>("STR_CombatDefendSelect"),
                SubPanelBaseBase.ButtonID.Sneak => FTKHub.Localized<TextInfo>("STR_CombatSneakSelect") + "(based on speed stat)",
                SubPanelBaseBase.ButtonID.Attempt => "attempt to complete this encounter", // cult device, devious enchanter (play)
                SubPanelBaseBase.ButtonID.BuyIn => "buy in NYI",
                SubPanelBaseBase.ButtonID.Collect => "collect NYI",
                SubPanelBaseBase.ButtonID.Devote => "devote NYI",
                SubPanelBaseBase.ButtonID.Drink => "drink from the well, you can throw gold in to increase your chance of success",
                SubPanelBaseBase.ButtonID.EndTurn => "leave this encounter and end turn",
                SubPanelBaseBase.ButtonID.Leave => "leave this encounter",
                SubPanelBaseBase.ButtonID.Enter => "enter this location",
                SubPanelBaseBase.ButtonID.Investigate => "investigate this encounter",
                SubPanelBaseBase.ButtonID.ThrowCoins => "spend gold to increase your chance of success",
                SubPanelBaseBase.ButtonID.Engage => FTKHub.Localized<TextInfo>("STR_CombatEngageSelect"),
                SubPanelBaseBase.ButtonID.Retreat => FTKHub.Localized<TextInfo>("STR_CombatRetreatSelect"),
                SubPanelBaseBase.ButtonID.Revive0 => FTKHub.Localized<TextMisc>("STR_ReviveMessage"),
                SubPanelBaseBase.ButtonID.Revive1 => FTKHub.Localized<TextMisc>("STR_ReviveMessage"),
                SubPanelBaseBase.ButtonID.Loot => "loot the body",
                SubPanelBaseBase.ButtonID.Give => "give gold to the stranger",
                _ => ""
            };
        }
    }
}