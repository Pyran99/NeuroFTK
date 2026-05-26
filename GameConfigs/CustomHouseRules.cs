namespace NeuroFTK.GameConfigs
{
    public class CustomHouseRules
    {
        // Sets custom difficulty for adventures
        public const bool SET_CUSTOM_RULES = true;
        public const float CHAOS_FREQUENCY = 8f; // higher = easier
        public const float LIFE_POOL = 6f; // higher = easier
        public const float ECONOMY_INFLATION = 80f; // lower = easier
        public const float GOLD_TARGET = 100f; // for gold rush adventure only
    }
}