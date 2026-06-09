using System.Collections.Generic;

namespace Pyran.NeuroFTK.GameConfigs;

public class CustomHouseRules
{
    public static bool SET_CUSTOM_RULES = true;

    public static Dictionary<string, CustomRuleValues> houseRules = new()
    {
        { "KillVexor", new CustomRuleValues(8f, 6f, 80f) },
        { "FrostAdventure", new CustomRuleValues(0f, 6f, 80f) }, // chaos is always 0
        { "Pirates", new CustomRuleValues(18f, 6f, 80f) }, // chaos is flood events
        { "DungeonCrawl", new CustomRuleValues(10f, 6f, 80f) },
        // Cellar cant customize
        { "HildebrantsCellar", new CustomRuleValues(8f, 6f, 80f) },
        // chaos & life dont change | co-op only
        { "GraveRobber", new CustomRuleValues(0f, 0f, 30f, 100f) },
        // DLC
        { "LostCiv", new CustomRuleValues(11f, 6f, 80f, 100f) },
    };

}

/// <summary>
/// values to set the house rule sliders
/// </summary>
/// <param name="chaos">higher = easier</param><param name="life">higher = easier</param><param name="inflation"> lower = easier</param><param name="gold">for gold rush (GraveRobber) adventure only</param>
public class CustomRuleValues(float chaos, float life, float inflation, float gold = 100f)
{
    //3-25
    public float chaosFrequency = chaos;
    //0-9
    public float lifePool = life;
    //3-15
    public float economyInflation = inflation;
    //3-15
    public float goldTarget = gold;
}