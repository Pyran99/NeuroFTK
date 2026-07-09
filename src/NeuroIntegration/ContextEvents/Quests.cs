using HarmonyLib;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class Quests
    {
        [HarmonyPatch(typeof(QuestLogicBase), nameof(QuestLogicBase.OnCompleteQuest))]
        [HarmonyPostfix]
        static void OnQuestComplete(QuestLogicBase __instance)
        {
            Plugin.Logger.LogWarning("def display name: " + __instance.m_QuestDef?.m_DisplayName);
            string msg = string.Concat(
            [
                __instance.m_QuestID, ", ", __instance.GetQuestType(true), " completed"
            ]);
            Plugin.Logger.LogWarning(msg);
            Context.Send(msg);
        }
    }
}