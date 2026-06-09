using System.Collections.Generic;

namespace Pyran.NeuroFTK.GameConfigs
{
    public class GlobalConfig
    {
        public static bool debug_mode = false;

        public readonly static Dictionary<string, object> defaultConfig = new()
        {
            { "environment_web_socket", "ws://localhost:8000" },
            { "debug_mode", false },
            { "use_custom_rules", CustomHouseRules.SET_CUSTOM_RULES },
            { "force_first_adventure", false },
        };
    }
}