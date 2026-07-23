using NeuroSdk;

namespace Pyran.NeuroFTK.Utils
{
    public static class StringMessages
    {
        public const string CultDeviceDestroyed = "you destroyed the evil cult device";
        public const string CultDeviceDestroyedFail = "you failed to destroy the evil cult device";
        public static readonly NeuroSdkFormatString ActionIssueOccured = "an issue occured with the {0} action";
        public const string HexContext = "[all tiles in range (displayed as [(position x,z) (name/realm)(quest)other info])] ";
        public static readonly NeuroSdkFormatString PortraitMsg = "{0} ({1}) says: {2}";
        public const string DungeonRolls = "(roll chances displayed as: character [button (description)] total successful rolls(chance for this result) = outcome result. (buttons with no roll results will always succeed))";
        public static readonly NeuroSdkFormatString DecisionButtonsPrompt = "[{0}] choose a character to perform the action with. if multiple characters can be chosen, only the character you choose to make the decision will act on it (collect will add to the chosen characters inventory, pass will skip for all characters, etc.). collected items can be sold at a market. discard should be avoided for most loot";
        public static readonly NeuroSdkFormatString RollSkillType = "these chances are based on your {0} stat";
        public const string BattleWon = "you have won the battle!";
        public static readonly NeuroSdkFormatString UnitDied = "{0} has died";
        public static readonly NeuroSdkFormatString UnitFled = "{0} has fled the battle";
        public static readonly NeuroSdkFormatString UnitTakeDamage = "{0} took {1} damage (health {2})";
        public static readonly NeuroSdkFormatString UnitHealed = "{0} healed {1} (health {2})";



    }
}