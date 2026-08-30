using System.Collections;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class DungeonRunSummary
    {

        [HarmonyPatch(typeof(uiSummaryScreen), "ShowCR")]
        [HarmonyPostfix]
        static IEnumerator Shown(IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
            string newRecord = uiSummaryScreen.Instance.m_NewRecord.gameObject.activeInHierarchy ? " (new record)" : "";
            Context.Send($"you cleared {uiSummaryScreen.Instance.m_RoomCountText.text} rooms{newRecord}!");
            ActionWindow window = ActionWindow.Create(uiSummaryScreen.Instance.m_MainPanel.gameObject);
            window.AddAction(new EndScreenAction(GameEndScreen.EndType.DungeonRun));
            window.SetForce(15, "end the dungeon run", "", true);
            UnregisterDisabledObject.QuickCreate(uiSummaryScreen.Instance.m_MainPanel.gameObject, window);
            window.Register();
        }
    }
}