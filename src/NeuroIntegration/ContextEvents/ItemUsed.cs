using FTKItemName;
using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    //TODO add ctx events when an item is used
    [HarmonyPatch]
    public class ItemUsed
    {
        [HarmonyPatch(typeof(scrollidentify), nameof(scrollidentify.OnUse))]
        [HarmonyPostfix]
        static void ScrollIdentify()
        {
            
        }
    }
}