using Google2u;

namespace Pyran.NeuroFTK.Utils
{
    public class EncounterButtonFlavor
    {
        public static string GetString(SubPanelBaseBase.ButtonID id)
        {
            return id switch
            {
                SubPanelBaseBase.ButtonID.Fight => FTKHub.Localized<TextInfo>("STR_CombatFightSelect"),
                SubPanelBaseBase.ButtonID.Ambush => FTKHub.Localized<TextInfo>("STR_CombatAmbushSelect"),
                SubPanelBaseBase.ButtonID.Defend => FTKHub.Localized<TextInfo>("STR_CombatDefendSelect"),
                SubPanelBaseBase.ButtonID.Sneak => FTKHub.Localized<TextInfo>("STR_CombatSneakSelect"),
                SubPanelBaseBase.ButtonID.Engage => FTKHub.Localized<TextInfo>("STR_CombatEngageSelect"),
                SubPanelBaseBase.ButtonID.Retreat => FTKHub.Localized<TextInfo>("STR_CombatRetreatSelect"),
                SubPanelBaseBase.ButtonID.Loot => "",
                SubPanelBaseBase.ButtonID.Journal => "",
                SubPanelBaseBase.ButtonID.Leave => "",
                SubPanelBaseBase.ButtonID.EndTurn => "",
                SubPanelBaseBase.ButtonID.Revive0 => "",
                SubPanelBaseBase.ButtonID.Revive1 => "",
                SubPanelBaseBase.ButtonID.Collect => "",
                SubPanelBaseBase.ButtonID.Drink => "",
                SubPanelBaseBase.ButtonID.ThrowCoins => "",
                SubPanelBaseBase.ButtonID.Train => "",
                SubPanelBaseBase.ButtonID.GetHealed => "",
                SubPanelBaseBase.ButtonID.BuyIn => "",
                SubPanelBaseBase.ButtonID.Attempt => "",
                SubPanelBaseBase.ButtonID.Investigate => "",
                SubPanelBaseBase.ButtonID.ViewWares => "",
                SubPanelBaseBase.ButtonID.UpgradePipe => "",
                SubPanelBaseBase.ButtonID.Rest => "",
                SubPanelBaseBase.ButtonID.Meditate => "",
                SubPanelBaseBase.ButtonID.Secure => "",
                SubPanelBaseBase.ButtonID.Enter => "",
                SubPanelBaseBase.ButtonID.PartyEnter => "",
                SubPanelBaseBase.ButtonID.SealHaunt => "",
                SubPanelBaseBase.ButtonID.Tribute => "",
                SubPanelBaseBase.ButtonID.Devote => "",
                SubPanelBaseBase.ButtonID.Give => "",
                _ => "", // gambles
            };
        }
    }
}