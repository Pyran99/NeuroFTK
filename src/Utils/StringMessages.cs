using NeuroSdk;

namespace Pyran.NeuroFTK.Utils
{
    public static class StringMessages
    {
        public const string CultDeviceDestroyed = "you destroyed the evil cult device";
        public const string CultDeviceDestroyedFail = "you failed to destroy the evil cult device";
        public const string HexContext = "## hexes to choose, organized by realm names, each displayed as: (position x,z), with optional context including any of quest name, encounter, distance. Empty hexes only show position. ";
        public const string DungeonRolls = "roll chances displayed as: character, list of buttons, total successful rolls(chance for this result) = outcome result. (buttons with no roll results will always succeed)";
        public const string BattleWon = "you have won the battle!";
        public const string GameOver = $"your team is all dead, you have lost this game. items can be purchased in the lore store to improve your combat capabilities.";
        public const string FocusDetails = $"Focus is a limited resource that is used to increase the success chance of some actions, use them wisely. The max amount you can spend will either be the characters amount or the number of outcomes for an action. send 0 or omit the property to not use focus, or if the action doesnt use focus any number you send wont be used.";
        public const string FocusUsage = $"focus is an optional property, ";
        public const string MarketQuery = $"buy items at the market or close the menu if there is nothing you want. If this character has a Two-Handed item in (RightHand), avoid equipping shields as this will unequip the weapon.";
        public const string OverworldReminderCtx = $"explore the map and fight enemies to increase your level. if you need a boat to travel somewhere, they can be purchased at any port. gold can be gained from fights and some encounters. positions are sent in vector 2 (x,z) format, if a location is out of range you can try to choose a destination with the nearest values as an attempt to get within range.";

        public static readonly NeuroSdkFormatString ItemUsed = "you used {0}";
        public static readonly NeuroSdkFormatString ItemUsedTargetHex = "select a hex to use {0} on";
        public static readonly NeuroSdkFormatString ItemUsedDestinationHex = "select a destination hex for {0}";
        public static readonly NeuroSdkFormatString ActionIssueOccured = "an issue occured with the {0} action";
        public static readonly NeuroSdkFormatString CriticalError = "an issue occured with {0}, tell vedal there is a problem";
        public static readonly NeuroSdkFormatString PortraitMsg = "{0} ({1}) says: {2}";
        public static readonly NeuroSdkFormatString DecisionButtonsPrompt = "[{0}] choose a character to perform the action with. if multiple characters can be chosen, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). discard should be avoided for most loot.";
        public static readonly NeuroSdkFormatString DecisionButtonsPromptMultiplayer = "[{0}] choose a character to perform the action with. Pass will allow another character to decide on this loot. discard should be avoided for most loot, prefer collect instead.";
        public static readonly NeuroSdkFormatString RollSkillType = "these chances are based on your {0} stat";
        public static readonly NeuroSdkFormatString UnitDied = "{0} has died";
        public static readonly NeuroSdkFormatString UnitFled = "{0} has fled the battle";
        public static readonly NeuroSdkFormatString UnitTakeDamage = "{0} took {1} damage (health {2})";
        public static readonly NeuroSdkFormatString UnitHealed = "{0} healed {1} (health {2})";
        public static readonly NeuroSdkFormatString StatusEffectApplied = "{0} ({1}) applied to {2}";
        public static readonly NeuroSdkFormatString StatusEffectRemoved = "{0} ({1}) removed from {2}";
        public static readonly NeuroSdkFormatString RollResults = "{0} rolled {1}/{2}";
        public static readonly NeuroSdkFormatString EncounterCost = "[encounter cost] {0} gold. you have {1} gold";
        public static readonly NeuroSdkFormatString CharacterEquipped = "{0} has {1}: {2}";




    }
}