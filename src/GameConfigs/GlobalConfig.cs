using System;
using System.Collections.Generic;

namespace Pyran.NeuroFTK.GameConfigs
{
    public class GlobalConfig
    {
        public static bool debugMode = false;
        public static bool gameInitialized = false;
        public static bool ForcedDefaultAdventure = false;
        public static bool IsMultiplayer { get; private set; } = false;
        public static bool FirstLoadResume { get; private set; } = true;

        public readonly static Dictionary<string, object> defaultConfig = new()
        {
            { "environment_web_socket", "ws://localhost:8000" },
            { "debug_mode", false },
            { "use_custom_rules", CustomHouseRules.SET_CUSTOM_RULES },
            { "force_first_adventure", false },
            { "is_multiplayer", false },
            { "launch_resume", true },
        };

        public static bool IsDebugMode() => debugMode;
        public static bool ResumeOnFirstLoad() => FirstLoadResume;
        public static bool ForcedFirstAdventure() => ForcedDefaultAdventure;

        public static void GameLoaded()
        {
            FirstLoadResume = false;
            gameInitialized = true;
        }

        public static void SetValues(Dictionary<string, object> _config)
        {
            Environment.SetEnvironmentVariable("NEURO_SDK_WS_URL", (string)_config["environment_web_socket"]);
            debugMode = (bool)_config["debug_mode"];
            CustomHouseRules.SET_CUSTOM_RULES = (bool)_config["use_custom_rules"];
            ForcedDefaultAdventure = (bool)_config["force_first_adventure"];
            IsMultiplayer = (bool)_config["is_multiplayer"];
            FirstLoadResume = (bool)_config["launch_resume"];
        }
    }
}