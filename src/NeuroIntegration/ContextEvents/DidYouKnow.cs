using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents
{
    [HarmonyPatch(typeof(uiDidYouKnow), nameof(uiDidYouKnow.SetRandomFact))]
    public class DidYouKnow
    {
        static void Postfix(uiDidYouKnow __instance)
        {
            Context.Send($"[Did you know] {__instance.m_Content.text}");
        }
    }
}