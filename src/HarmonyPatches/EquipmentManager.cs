using HarmonyLib;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class EquipmentManager
    {
        static void Test1(CharacterOverworld cow)
        {
            PlayerInventory inv = cow.m_PlayerInventory;
            ItemContainer backpack = inv.m_ContainerBackpack;
            bool helmet = inv.m_ContainerHead.IsEmpty();
        }
    }
}