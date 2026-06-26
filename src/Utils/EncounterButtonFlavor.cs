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
                SubPanelBaseBase.ButtonID.Loot => "loot NYI",
                SubPanelBaseBase.ButtonID.Journal => "journal NYI",
                SubPanelBaseBase.ButtonID.Leave => "leave NYI",
                SubPanelBaseBase.ButtonID.EndTurn => "endTurn NYI",
                SubPanelBaseBase.ButtonID.Revive0 => "revive0 NYI",
                SubPanelBaseBase.ButtonID.Revive1 => "revive1 NYI",
                SubPanelBaseBase.ButtonID.Collect => "collect NYI",
                SubPanelBaseBase.ButtonID.Drink => "drink NYI",
                SubPanelBaseBase.ButtonID.ThrowCoins => "throwCoins NYI",
                SubPanelBaseBase.ButtonID.Train => "train NYI",
                SubPanelBaseBase.ButtonID.GetHealed => "getHealed NYI",
                SubPanelBaseBase.ButtonID.BuyIn => "buyIn NYI",
                SubPanelBaseBase.ButtonID.Attempt => "attempt NYI",
                SubPanelBaseBase.ButtonID.Investigate => "investigate NYI",
                SubPanelBaseBase.ButtonID.ViewWares => "viewWares NYI",
                SubPanelBaseBase.ButtonID.UpgradePipe => "upgradePipe NYI",
                SubPanelBaseBase.ButtonID.Rest => "rest NYI",
                SubPanelBaseBase.ButtonID.Meditate => "meditate NYI",
                SubPanelBaseBase.ButtonID.Secure => "secure NYI",
                SubPanelBaseBase.ButtonID.Enter => "enter NYI",
                SubPanelBaseBase.ButtonID.PartyEnter => "partyEnter NYI",
                SubPanelBaseBase.ButtonID.SealHaunt => "sealHaunt NYI",
                SubPanelBaseBase.ButtonID.Tribute => "tribute NYI",
                SubPanelBaseBase.ButtonID.Devote => "devote NYI",
                SubPanelBaseBase.ButtonID.Give => "give NYI",
                _ => "", // gambles
            };
        }
    }
}