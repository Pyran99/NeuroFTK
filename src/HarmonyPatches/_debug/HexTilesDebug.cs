using HarmonyLib;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.NeuroIntegration.Actions;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class HexTilesTesting
    {
        static HexLand currentHover;

        // left or right clicks
        [HarmonyPatch(typeof(Movement), "TrackCheckClickPath")]
        [HarmonyPostfix]
        static void ClickTarget(HexLand _hexland)
        {
            currentHover = _hexland;
            Plugin.Logger.LogMessage($"check click path: {_hexland.GetHexLandID().m_BigIndex} - {_hexland.GetHexLandID().m_SmallIndex}");
        }

        [HarmonyPatch(typeof(Movement), "TrackCheckHoverPath")]
        [HarmonyPostfix]
        static void ValidOnHover(HexLand _hexland)
        {
            if (currentHover == _hexland) return;
            currentHover = _hexland;
            if (!Plugin.doSpam || !GlobalConfig.debug_mode) return;
            if (MovementAction.hexPositions.ContainsKey(_hexland.GetPosition().ToString()))
            {
                Plugin.Logger.LogMessage($"valid id {_hexland.GetHexLandID().m_BigIndex} - {_hexland.GetHexLandID().m_SmallIndex}");
            }
        }

        // // no calls, maybe controller?
        // [HarmonyPatch(typeof(Movement), "TrackHexPickHover")]
        // [HarmonyPostfix]
        // static void Test2(HexLand hexLand)
        // {
        //     if (hexLand != currentHover)
        //     {
        //         currentHover = hexLand;
        //         Plugin.Logger.LogWarning($"hex pick HOVER {hexLand.GetHexLandID().m_BigIndex} - {hexLand.GetHexLandID().m_SmallIndex}");
        //     }
        // }

        // [HarmonyPatch(typeof(Movement), "TrackHexPickClick")]
        // [HarmonyPostfix]
        // static void Test3()
        // {
        //     Plugin.Logger.LogWarning("hex pick click");
            
        // }
    }
}
