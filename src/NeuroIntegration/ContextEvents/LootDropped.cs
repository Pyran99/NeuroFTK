using Google2u;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;

namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents
{
    [HarmonyPatch]
    public class LootDropped
    {
        [HarmonyPatch(typeof(EncounterSession), nameof(EncounterSession.DisplayLootItem))]
        [HarmonyPostfix]
        static void CtxDisplayedLootItem(string _item, int ___m_LootItemCount, string ___m_LootItem)
        {
            Plugin.Logger.LogWarning("DisplayLootItem: " + _item);
            FTK_itembase.ID id = FTK_itembase.GetEnum(_item);
            FTK_itembase itemBase = FTK_itembase.GetItemBase(id);
            string name = itemBase.GetLocalizedName();
            string rarity = FTKHub.Localized<TextMisc>(FTK_itemRarityLevelDB.GetDB().GetEntry(itemBase.m_ItemRarity).m_Display);
            string description;
            string amount = "";
            bool hasAmount = false;
            if (___m_LootItem.Contains("_gold_") || ___m_LootItem.Contains("_lore_")) hasAmount = true;
            if (hasAmount && ___m_LootItemCount > 0) amount = $"(x{___m_LootItemCount})";
            description = ItemData.GetItemDescription(id, true, GameLogic.Instance.GetCurrentCOW());

            Context.Send($"[Loot] {name}{amount} (Rarity) {StringReplace.RemoveStyling(rarity)} (Description) {description}");
            // [the loot item to decide on]Gold Coins [Rarity]Common [Description]Currency of Fahrul. Each coin worth its weight in gold.
        }
    }
}