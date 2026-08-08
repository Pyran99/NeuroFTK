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
        [HarmonyPostfix]
        static void OnItemUsed(FTK_itembase.ID _item, CharacterOverworld __instance)
        {
            Context.Send($"{CharacterData.GetCharacterName(__instance)} used {ItemData.GetItemName(_item)}", true);
        }
        
        [HarmonyPatch(typeof(scrollidentify), nameof(scrollidentify.OnUse))]
        [HarmonyPostfix]
        static void ScrollIdentify()
        {
            
        }

        [HarmonyPatch(typeof(GameFlow), nameof(GameFlow.SpawnTreasureMapChest))]
        [HarmonyPostfix]
        static void OnTreasureSpawned(HexLandID _hexID)
        {
            Context.Send($"a treasure chest appeared at {HexData.GetVec2Pos(FTKHex.Instance.GetHexLand(_hexID))}");
        }
    }
}