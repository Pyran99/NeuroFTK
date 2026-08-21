using FTKItemName;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    // items at FTKItemName
    [HarmonyPatch]
    public class ItemUsed
    {

        [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.CharacterUseItemRPC))]
        [HarmonyPrefix]
        static void OnItemUsed(FTK_itembase.ID _item, CharacterOverworld __instance)
        {
            Context.Send($"{CharacterData.GetCharacterName(__instance)} used {ItemData.GetItemName(_item)}", true);
        }

        [HarmonyPatch(typeof(FTKItem), nameof(FTKItem.OnUse))]
        [HarmonyPrefix]
        static void FtkItemUsed(FTK_itembase.ID ___m_ItemID, CharacterOverworld _cow)
        {
            Plugin.Logger.LogMessage($"{CharacterData.GetCharacterName(_cow)} used {ItemData.GetItemName(___m_ItemID)}");
            HexPick.itemUsed = FTK_itembase.ID.None;
            if (!Multiplayer.IsYourCow(_cow)) return;
            if (FTKItem.Get(___m_ItemID) is Movement.IPickHexClient) HexPick.itemUsed = ___m_ItemID;
        }

        [HarmonyPatch(typeof(GameFlow), nameof(GameFlow.SpawnTreasureMapChest))]
        [HarmonyPostfix]
        static void OnTreasureSpawned(HexLandID _hexID)
        {
            Context.Send($"a treasure chest appeared at {HexData.GetVec2Pos(FTKHex.Instance.GetHexLand(_hexID))}");
        }

        [HarmonyPatch(typeof(MiscManager), nameof(MiscManager.CreateTwoPortalsRPC))]
        [HarmonyPostfix]
        static void CreatedPortals(HexLandID _portalID1, HexLandID _portalID2)
        {
            Context.Send($"portals created between {HexData.GetVec2Pos(FTKHex.Instance.GetHexLand(_portalID1))} and {HexData.GetVec2Pos(FTKHex.Instance.GetHexLand(_portalID2))}");
        }
    }
}