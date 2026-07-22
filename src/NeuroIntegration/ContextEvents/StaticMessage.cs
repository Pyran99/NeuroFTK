using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class StaticMessage
    {
        [HarmonyPatch(typeof(uiStaticMessageHud), nameof(uiStaticMessageHud.ShowStaticMessage))]
        [HarmonyPrefix]
        static void ShowMessage(string _primary, string _secondary)
        {
            Context.Send($"{_primary} {_secondary}");
        }
    }
}