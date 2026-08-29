using System;
using System.Collections.Generic;
using System.Text;
using Pyran.NeuroFTK.HarmonyPatches;

namespace Pyran.NeuroFTK.GameConfigs
{
    public class GlobalConfig
    {
        public static bool debugMode = false;
        public static bool gameInitialized = false;
        public static bool ForceSpecificAdventure = false;
        public static string AdventureCode = "ftk";
        public static bool IsMultiplayer { get; private set; } = false;
        public static bool FirstLoadResume { get; private set; } = true;
        public static bool AllowCheats { get; private set; } = false;
        public static int MaxHexSearch { get; private set; } = 100;
        public static float maxDistance = 2.8866f * 15f;

        public readonly static Dictionary<string, object> defaultConfig = new()
        {
            { "environment_web_socket", "ws://localhost:8000" },
            { "allow_cheats", false },
            { "debug_mode", false },
            { "use_custom_rules", CustomHouseRules.SET_CUSTOM_RULES },
            { "is_multiplayer", false },
            { "launch_resume", true },
            { "max_hex_search", 100 },
            { "force_custom_adventure", false },
            { "custom_adventure_code", "ftk" },
        };

        public static bool IsDebugMode() => debugMode;
        public static bool ResumeOnFirstLoad() => FirstLoadResume;
        public static bool ForcedCustomAdventure() => ForceSpecificAdventure;

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
            ForceSpecificAdventure = (bool)_config["force_custom_adventure"];
            AdventureCode = (string)_config["custom_adventure_code"];
            if (!ConfigureAdventure.adventureCodes.ContainsKey(AdventureCode))
            {
                Plugin.Logger.LogError($"invalid adventure code: {AdventureCode}");
                AdventureCode = "ftk";
            }
            IsMultiplayer = (bool)_config["is_multiplayer"];
            FirstLoadResume = (bool)_config["launch_resume"];
            AllowCheats = (bool)_config["allow_cheats"];
            MaxHexSearch = Convert.ToInt32(_config["max_hex_search"]);

            StringBuilder sb = new();
            sb.AppendLine($"Config set:");
            foreach (KeyValuePair<string, object> kvp in _config)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            Plugin.Logger.LogInfo(sb.ToString());
        }
    }
}