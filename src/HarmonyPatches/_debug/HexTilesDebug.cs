using HarmonyLib;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class HexTilesTesting
    {
        static HexLand currentHover;

        // // left or right clicks
        // [HarmonyPatch(typeof(Movement), "TrackCheckClickPath")]
        // [HarmonyPostfix]
        // static void ClickTarget(HexLand _hexland)
        // {
        //     currentHover = _hexland;
        // }

        // [HarmonyPatch(typeof(Movement), "TrackCheckHoverPath")]
        // [HarmonyPostfix]
        // static void ValidOnHover(HexLand _hexland)
        // {
        //     if (currentHover == _hexland) return;
        //     currentHover = _hexland;
        // }

        // // was testing path finding
        // static void Test()
        // {
        //     Plugin.Logger.LogWarning("testing quest hex movement");
        //     List<HexLand> hexes = [];
        //     foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_StoryQuestRoot.GetComponentsInChildren<uiQuestItem>())
        //     {
        //         QuestLogicBase quest = q.m_Quest;
        //         if (quest == null || quest.IsConsiderComplete()) continue;
        //         hexes.Add(quest.GetHexLandDestination());
        //     }
        //     foreach (uiQuestItem q in uiGameTrackerHUD.Instance.m_SideQuestRoot.GetComponentsInChildren<uiQuestItem>())
        //     {
        //         QuestLogicBase quest = q.m_Quest;
        //         if (quest == null || quest.IsConsiderComplete()) continue;
        //         hexes.Add(quest.GetHexLandDestination());
        //     }
        //     HexLand target = hexes[Random.Range(0, hexes.Count)];
        //     if (target == null) return;
        //     Plugin.Logger.LogWarning("max dist = " + 2.8866f * 15f);
        //     Plugin.Logger.LogWarning("dist = " + (target.GetPosition() - CharacterData.GetActiveCow().GetHexLand().GetPosition()).magnitude);
        //     // OverworldFlow.ReverseUpdateHexMove(Movement.Instance);
        //     OverworldFlow.ReverseClearDrawPath(Movement.Instance, Movement.Instance.m_HexListPartial);
        //     Plugin.Logger.LogWarning("target1 = " + target?.GetPosition());
        //     Plugin.Logger.LogWarning("last partial1 = " + Movement.Instance.m_HexListPartial.Last()?.GetPosition());
        //     OverworldFlow.ReverseCheckHoverPath(Movement.Instance, target);
        //     List<HexLand> temp = [];
        //     HexLand.FindPath(CharacterData.GetActiveCow().GetHexLand(), target, HexLand.PathFindingStartState.OnLand, ref temp);
        //     Plugin.Logger.LogWarning("target2 = " + target?.GetPosition());
        //     Plugin.Logger.LogWarning("last partial2 = " + Movement.Instance.m_HexListPartial.Last()?.GetPosition());
        //     OverworldFlow.ReverseUpdateHexMove(Movement.Instance);
        // }
        
    }
}
