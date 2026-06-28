using Google2u;

namespace Pyran.NeuroFTK.Utils
{
    public class EncounterButton
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
                SubPanelBaseBase.ButtonID.Revive0 => FTKHub.Localized<TextMisc>("STR_ReviveMessage"),
                SubPanelBaseBase.ButtonID.Revive1 => FTKHub.Localized<TextMisc>("STR_ReviveMessage"),
                _ => ""
            };
        }
    }
}