using System.Collections.Generic;
using System.Linq;
using FTKItemName;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.GameConfigs;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class HexPick
    {
        static ActionWindow window;
        public static FTK_itembase.ID itemUsed = FTK_itembase.ID.None;

        [HarmonyPatch(typeof(Movement), nameof(Movement.StartPickHex))]
        [HarmonyPostfix]
        static void StartPickHex()
        {
            OverworldFlow.StopTracking();
            CreateNeuroAction();
        }

        [HarmonyPatch(typeof(Movement), nameof(Movement.PickHexCancelled))]
        [HarmonyPostfix]
        static void EndPickHex()
        {
            FinishedPicking();
        }

        [HarmonyPatch(typeof(Movement), nameof(Movement.PickHexComplete))]
        [HarmonyPostfix]
        static void CompletePickHex()
        {
            FinishedPicking();
        }

        [HarmonyPatch(typeof(HexLand), nameof(HexLand.SetPickRangeOn))]
        [HarmonyPostfix]
        static void PickHexRange()
        {
            // OverworldFlow.StopTracking();
            // CreateNeuroAction();
        }

        // manual pick call
        [HarmonyPatch(typeof(Movement), "TrackHexPickClick")]
        [HarmonyReversePatch]
        public static void ReverseHexPick(object instance, HexLand hexLand, bool _isOk)
        {
        }

        // manual pick call
        [HarmonyPatch(typeof(Movement), "TrackHexPickHover")]
        [HarmonyReversePatch]
        public static void ReverseHexHover(object instance, HexLand hexLand)
        {
        }

        [HarmonyPatch(typeof(uiTownServiceMenu), "StartBoatReclaim")]
        [HarmonyPrefix]
        static void OnBoatReclain()
        {
            Context.Send($"select a nearby boat to pickup and store in your backpack");
        }

        public static void PickHex(HexLand hex)
        {
            ReverseHexHover(Movement.Instance, hex);
            ReverseHexPick(Movement.Instance, hex, true);
        }

        static void FinishedPicking()
        {
            Object.Destroy(window);
        }

        static void CreateNeuroAction()
        {
            if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
            Dictionary<string, HexLand> tiles = InRangeDrawer.gPickRadiusHexList.ToDictionary(x => HexData.GetVec2Pos(x).ToString(), x => x);
            Plugin.Logger.LogWarning($"found {tiles.Count} tiles");
            if (tiles.Count > GlobalConfig.MaxHexSearch)
            {
                tiles = (Dictionary<string, HexLand>)tiles.Take(GlobalConfig.MaxHexSearch);
                Plugin.Logger.LogWarning($"removed count = {tiles.Count}");
            }
            string ctx = OverworldFlow.GetTileContext(InRangeDrawer.gPickRadiusHexList, true);
            if (itemUsed != FTK_itembase.ID.None)
            {
                List<string> toRemove = [];
                if (itemUsed == FTK_itembase.ID.scrollvision)
                {
                    //does not create list
                    HexLand randHex = FTKHex.Instance.m_AllLandHexes[Random.Range(0, FTKHex.Instance.m_AllLandHexes.Count)];
                    if (randHex == null) Plugin.Logger.LogError("WTF");
                    Context.Send($"revealing hexes around {HexData.GetVec2Pos(randHex)}");
                    QuickTimerCallback timer = new(() => PickHex(randHex), Movement.Instance.gameObject, 0.5f);
                    return;
                }
                else if (itemUsed == FTK_itembase.ID.scrollpurify)
                {
                    foreach (KeyValuePair<string, HexLand> kvp in tiles)
                    {
                        if (!HexData.IsHexCorrupted(kvp.Value)) toRemove.Add(kvp.Key);
                    }
                    foreach (string key in toRemove) tiles.Remove(key);
                    toRemove.Clear();
                }
                FTKItem item = FTKItem.Get(itemUsed);
                if (item is Movement.IPickHexClient)
                {
                    FTKPickHexItem pickItem = item as FTKPickHexItem;
                    foreach (KeyValuePair<string, HexLand> kvp in tiles)
                    {
                        if (!pickItem.PickHexValidCallback(kvp.Value))
                        {
                            toRemove.Add(kvp.Key);
                        }
                    }
                    foreach (string key in toRemove) tiles.Remove(key);
                    toRemove.Clear();
                }
            }
            if (tiles.Count == 0)
            {
                string msg = $"there were no hexes to pick for {ItemData.GetItemName(itemUsed)}";
                Plugin.Logger.LogError(msg);
                Context.Send(msg);
                Movement.Instance.PickHexCancelled();
                QuickTimerCallback timer2 = new(OverworldFlow.BeginMovementTurn, Movement.Instance.gameObject);
                return;
            }
            QuickTimerCallback timer3 = new(() => Create(ctx, tiles), Movement.Instance.gameObject, 0.5f);
        }

        static void Create(string ctx, Dictionary<string, HexLand> tiles)
        {
            window = PickHexAction.CreateWindow(Movement.Instance.m_CharacterOverworld, ctx, tiles);
        }

        [HarmonyPatch(typeof(FTKItem), nameof(FTKItem.OnUse))]
        [HarmonyPrefix]
        static void ItemUsed(FTK_itembase.ID ___m_ItemID)
        {
            if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
            Plugin.Logger.LogMessage($"item used {ItemData.GetItemName(___m_ItemID)}");
            itemUsed = ___m_ItemID;
        }

        // static void SendCtx(FTK_itembase.ID id)
        // {
        //     switch (id)
        //     {
        //         case FTK_itembase.ID.boat:
        //         case FTK_itembase.ID.boatA:
        //         case FTK_itembase.ID.boatB:
        //         case FTK_itembase.ID.boatD:
        //         case FTK_itembase.ID.boatE:
        //             Context.Send(StringMessages.ItemUsedTargetHex.Format(["boat"]));
        //             break;
        //         case FTK_itembase.ID.scrollidentify:
        //             Context.Send(StringMessages.ItemUsedTargetHex.Format(["identify scroll"]));
        //             break;
        //         case FTK_itembase.ID.scrollpurify:
        //             Context.Send(StringMessages.ItemUsedTargetHex.Format(["purify scroll"]));
        //             break;
        //         case FTK_itembase.ID.scrollgroupteleport:
        //             Context.Send(StringMessages.ItemUsedDestinationHex.Format(["group teleport"]));
        //             break;
        //         case FTK_itembase.ID.scrollportal:
        //             Context.Send(StringMessages.ItemUsedDestinationHex.Format(["portal"]));
        //             break;
        //         case FTK_itembase.ID.scrollteleport:
        //             Context.Send(StringMessages.ItemUsedDestinationHex.Format(["teleport"]));
        //             break;
        //         default:
        //             Context.Send(StringMessages.ItemUsed.Format([ItemData.GetItemName(id)]));
        //             break;
        //     }
        // }

        // [HarmonyPatch(typeof(boat), nameof(boat.OnUse))]
        // [HarmonyPrefix]
        // static void OnUseBoat()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedTargetHex.Format(["boat"]));
        // }

        // [HarmonyPatch(typeof(scrollgroupteleport), nameof(scrollgroupteleport.OnUse))]
        // [HarmonyPrefix]
        // static void OnUseGroupTeleport()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedDestinationHex.Format(["group teleport"]));
        // }

        // [HarmonyPatch(typeof(scrollidentify), nameof(scrollidentify.OnUse))]
        // [HarmonyPrefix]
        // static void OnUseIdentify()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedTargetHex.Format(["identify scroll"]));
        // }

        // [HarmonyPatch(typeof(scrollportal), nameof(scrollportal.OnUse))]
        // [HarmonyPrefix]
        // static void OnUsePortal()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedDestinationHex.Format(["portal"]));
        // }

        // [HarmonyPatch(typeof(scrollpurify), nameof(scrollpurify.OnUse))]
        // [HarmonyPrefix]
        // static void OnUsePurify()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedTargetHex.Format(["purify scroll"]));
        // }

        // [HarmonyPatch(typeof(scrollteleport), nameof(scrollteleport.OnUse))]
        // [HarmonyPrefix]
        // static void OnUseTeleport()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedDestinationHex.Format(["teleport"]));
        // }

        // [HarmonyPatch(typeof(scrollvision), nameof(scrollvision.OnUse))]
        // [HarmonyPrefix]
        // static void OnUseVision()
        // {
        //     if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld)) return;
        //     Context.Send(StringMessages.ItemUsedTargetHex.Format(["vision scroll"]));
        // }
    }
}