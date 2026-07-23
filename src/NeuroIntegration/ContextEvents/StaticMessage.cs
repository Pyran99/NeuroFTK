using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class StaticMessage
    {
        public static string Message { get; private set; } = "";

        [HarmonyPatch(typeof(uiStaticMessageHud), nameof(uiStaticMessageHud.ShowStaticMessage))]
        [HarmonyPrefix]
        static void ShowMessage(string _primary, string _secondary)
        {
            Message = $"{_primary} {_secondary}";
        }

        [HarmonyPatch(typeof(uiStaticMessageHud), nameof(uiStaticMessageHud.DisableStaticMessage))]
        [HarmonyPostfix]
        static void HideMessage()
        {
            Message = "";
        }
    }
}