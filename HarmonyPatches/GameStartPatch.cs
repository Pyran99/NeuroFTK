using HarmonyLib;

namespace NeuroFTK.HarmonyPatches
{
    // log order -> 2, 1, 4 (understood popup), 5, 3
    [HarmonyPatch]
    public class GameStartPatch
    {
        [HarmonyPatch(typeof(GameStart), "Start")]
        [HarmonyPostfix]
        static void AfterStart()
        {
            Plugin.Logger.LogMessage("4. after game start");
        }

        [HarmonyPatch(typeof(uiStartGame), nameof(uiStartGame.ShowStartPage))]
        [HarmonyPostfix]
        static void AfterShowStartPage()
        {
            Plugin.Logger.LogMessage("3. after show start page");
        }

        [HarmonyPatch(typeof(SplashScreen), "Start")]
        [HarmonyPostfix]
        static void AfterSplashScreenStart()
        {
            Plugin.Logger.LogMessage("1. after splash screen start");
        }

        [HarmonyPatch(typeof(SplashScreen), "DisplayScene")]
        [HarmonyPostfix]
        static void AfterSplashScreenDisplaySceneIEnumerator()
        {
            Plugin.Logger.LogMessage("2. after splash screen enumerator");
        }

        [HarmonyPatch(typeof(StartGameFE.MainScreen), nameof(StartGameFE.MainScreen.Show))]
        [HarmonyPostfix]
        static void AfterMainScreenShow()
        {
            Plugin.Logger.LogMessage("5. after main screen show");
        }

    }
}