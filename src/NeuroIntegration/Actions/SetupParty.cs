using HarmonyLib;

namespace NeuroFTK.NeuroIntegration.Actions
{
    [HarmonyPatch]
    public class SetupParty
    {
        /*
        send current characters class info context to neuro
        randomize for new classes
        when finished choose 3 names
        start game
        */
        [HarmonyPatch(typeof(uiStartGame), nameof(uiStartGame.ShowCreateCharacter))]
        [HarmonyPostfix]
        static void PH()
        {
            
        }


        static void ActionStartGame(uiStartGame _instance)
        {
            Plugin.Logger.LogMessage("start game action " + nameof(_instance.StartGame));
            // _instance.StartGame();
        }

    }
}