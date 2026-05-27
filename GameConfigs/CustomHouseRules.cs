using System.Collections.Generic;

namespace NeuroFTK.GameConfigs
{
    public class CustomHouseRules
    {
        public const bool SET_CUSTOM_RULES = true;

        public static Dictionary<string, CustomRuleValues> houseRules = new()
        {
            { "KillVexor", new CustomRuleValues(8f, 6f, 80f) },
            { "FrostAdventure", new CustomRuleValues(0f, 6f, 80f) }, // chaos is always 0
            { "Pirates", new CustomRuleValues(18f, 6f, 80f) }, // chaos is flood events
            { "DungeonCrawl", new CustomRuleValues(10f, 6f, 80f) },
            // Cellar cant customize
            // { "HildebrantsCellar", new CustomRuleValues(8f, 6f, 80f) },
            // chaos & life dont change
            { "GraveRobber", new CustomRuleValues(0f, 0f, 30f, 100f) },
            { "LostCiv", new CustomRuleValues(8f, 6f, 80f, 100f) },
        };

    }

    /// <summary>
    /// values to set the house rule sliders
    /// </summary>
    /// <param name="chaos">higher = easier</param>
    /// <param name="life">higher = easier</param>
    /// <param name="inflation"> lower = easier</param>
    /// <param name="gold">for gold rush (GraveRobber) adventure only</param>
    public class CustomRuleValues(float chaos, float life, float inflation, float gold = 100f)
    {
        public float chaosFrequency = chaos;
        public float lifePool = life;
        public float economyInflation = inflation;
        public float goldTarget = gold;
    }
}