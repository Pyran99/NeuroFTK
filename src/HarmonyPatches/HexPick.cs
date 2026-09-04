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
        public static FTK_itembase.ID itemUsed = FTK_itembase.ID.None;
        
        static ActionWindow window;
        static bool boatReclaim = false;
        static readonly bool sendPoiCtx = true;

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

        [HarmonyPatch(typeof(uiTownServiceMenu), "StartBoatReclaim")]
        [HarmonyPrefix]
        static void OnBoatReclain()
        {
            Context.Send($"select a nearby boat to pickup and store in your backpack", true);
            boatReclaim = true;
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

        public static void PickHex(HexLand hex, bool ok = true)
        {
            FTKItem item = FTKItem.Get(itemUsed);
            if (item is Movement.IPickHexClient)
            {
                if (!(item as Movement.IPickHexClient).PickHexValidCallback(hex))
                {
                    Plugin.Logger.LogError($"chosen hex is not valid for scroll item {itemUsed}");
                    Context.Send($"chosen hex is not valid for scroll item {itemUsed}");
                    Movement.Instance.PickHexCancelled();
                    return;
                }
            }
            ReverseHexHover(Movement.Instance, hex);
            ReverseHexPick(Movement.Instance, hex, ok);
        }

        static void FinishedPicking()
        {
            Object.Destroy(window);
            Reset();
        }

        static void CreateNeuroAction()
        {
            if (!Multiplayer.IsYourCow(Movement.Instance.m_CharacterOverworld))
            {
                Reset();
                return;
            }
            Dictionary<string, HexLand> tiles = [];
            List<HexLand> pois = [];
            FTKItem item = null;
            string errMsg = "";
            CharacterOverworld cow = CharacterData.GetActiveCow();
            HexLand cowHex = cow.GetHexLand();
            if (boatReclaim)
            {
                MiniHexInfo port = Movement.Instance.m_CharacterOverworld.GetHexLand().GetPOI();
                if (port is MiniHexUtility)
                {
                    List<HexLand> neighbors = [.. port.m_HexLand.m_Neighbors];
                    foreach (HexLand tile in neighbors)
                    {
                        if (tile.HasPOI())
                        {
                            MiniHexInfo poi = tile.GetPOI();
                            if (poi is not MiniHexBoat) continue;
                            if ((poi as MiniHexBoat).IsBoatDamaged())
                            {
                                errMsg += $"nearby boat at {HexData.GetVec2Pos(tile)} must be repaired first.";
                                continue;
                            }
                            tiles.Add(HexData.GetVec2Pos(tile).ToString(), tile);
                        }
                    }
                    if (tiles.Count == 0) errMsg += " there were no boats to pick up, if there are damaged boats nearby they must be repaired first.";
                }
            }
            else if (itemUsed != FTK_itembase.ID.None)
            {
                List<string> toRemove = [];
                // does not create list
                item = FTKItem.Get(itemUsed);
                if (itemUsed == FTK_itembase.ID.scrollvision) // auto selecting random hex
                {
                    HexLand randHex = FTKHex.Instance.m_AllLandHexes[Random.Range(0, FTKHex.Instance.m_AllLandHexes.Count)];
                    if (randHex == null) Plugin.Logger.LogError("WTF");
                    Context.Send($"revealing hexes around {HexData.GetVec2Pos(randHex)}");
                    Movement.Instance.StartCoroutine(QuickTimerCallback.WaitRoutine(() => PickHex(randHex), Movement.Instance.gameObject, 0.5f));
                    Reset();
                    return;
                }
                else if (itemUsed == FTK_itembase.ID.scrollpurify)
                {
                    foreach (HexLand hex in InRangeDrawer.gPickRadiusHexList)
                    {
                        if (HexData.IsHexCorrupted(hex)) tiles.Add(HexData.GetVec2Pos(hex).ToString(), hex);
                    }
                }
                else if (itemUsed == FTK_itembase.ID.scrollportal)
                {
                    FTKPickHexItem pickItem = item as FTKPickHexItem;
                    foreach (HexLand hex in InRangeDrawer.gPickRadiusHexList)
                    {
                        if (hex.HasPOI()) continue;
                        if (pickItem.PickHexValidCallback(hex))
                        {
                            if (HexLand.Distance(cowHex, hex) < 2.1) continue;
                            tiles.Add(HexData.GetVec2Pos(hex).ToString(), hex);
                        }
                    }
                    foreach (HexLand hex in HexData.GetAllTilesWithinRange(6, cowHex, cow))
                    {
                        if (hex.HasPOI()) pois.Add(hex);
                    }
                }
                else if (itemUsed == FTK_itembase.ID.scrollteleport || itemUsed == FTK_itembase.ID.scrollgroupteleport)
                {
                    FTKPickHexItem pickItem = item as FTKPickHexItem;
                    foreach (HexLand hex in InRangeDrawer.gPickRadiusHexList)
                    {
                        if (hex.HasPOI()) continue;
                        if (HexLand.Distance(cowHex, hex) < 2.1) continue;
                        tiles.Add(HexData.GetVec2Pos(hex).ToString(), hex);
                        // if (pickItem.PickHexValidCallback(hex))
                        // {
                        // }
                    }
                    foreach (HexLand hex in HexData.GetAllTilesWithinRange(6, cowHex, cow))
                    {
                        if (hex.HasPOI()) pois.Add(hex);
                    }
                }
                else if (item is Movement.IPickHexClient)
                {
                    FTKPickHexItem pickItem = item as FTKPickHexItem;
                    foreach (HexLand hex in InRangeDrawer.gPickRadiusHexList)
                    {
                        tiles.Add(HexData.GetVec2Pos(hex).ToString(), hex);
                    }
                }
                if (tiles.Count == 0) errMsg += $"there were no hexes to pick for {ItemData.GetItemName(itemUsed)}";
            }
            Plugin.Logger.LogWarning($"found {tiles.Count} pick tiles");
            if (tiles.Count > GlobalConfig.MaxHexSearch)
            {
                tiles = tiles.Take(GlobalConfig.MaxHexSearch).ToDictionary(x => x.Key, x => x.Value);
                Plugin.Logger.LogWarning($"removed above limit of {GlobalConfig.MaxHexSearch} tiles");
            }
            if (tiles.Count == 0)
            {
                Context.Send(errMsg);
                Reset();
                OverworldFlow.cancelBoatReclaim = true;
                Movement.Instance.StartCoroutine( QuickTimerCallback.WaitRoutine(DelayCancel, Movement.Instance.gameObject, 1.5f));
                return;
            }
            if (sendPoiCtx && pois.Count > 0)
            {
                string poiCtx = "## POIs in range (you cannot choose these directly. If you want to move to one of these, select a position with close x/z values). \n" + OverworldFlow.GetTileContext(pois, true, false);
                Context.Send(poiCtx, true);
            }
            string ctx = OverworldFlow.GetTileContext([.. tiles.Select(x => x.Value)], true);
            Movement.Instance.StartCoroutine( QuickTimerCallback.WaitRoutine(() => Create(ctx, tiles, itemUsed), Movement.Instance.gameObject, 0.5f));
            Reset();
        }

        static void DelayCancel()
        {
            OverworldFlow.cancelBoatReclaim = false;
            PickHex(CharacterData.GetActiveCow().GetHexLand(), false);
        }

        static void Reset()
        {
            itemUsed = FTK_itembase.ID.None;
            boatReclaim = false;
        }

        static void Create(string ctx, Dictionary<string, HexLand> tiles, FTK_itembase.ID _item)
        {
            window = PickHexAction.CreateWindow(Movement.Instance.m_CharacterOverworld, ctx, tiles, _item);
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
    }
}