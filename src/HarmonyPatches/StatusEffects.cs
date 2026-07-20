using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class StatusEffects
    {

        readonly Dictionary<string, List<GameObject>> immunities = [];
        readonly Dictionary<string, List<GameObject>> ailments = [];
        
        [HarmonyPatch(typeof(uiPlayerMainHudStatus), nameof(uiPlayerMainHudStatus.SetStatusIcons))] //1. each on game start | 2. turn start, overworld: before tracking
        [HarmonyPostfix]
        static void OnSetIcons(uiPlayerMainHudStatus __instance)
        {
            // uiPlayerMainHud\DisplayRoot\playerMainHudStatus
            uiPlayerMainHud hud = __instance.transform.parent.transform.parent.GetComponent<uiPlayerMainHud>();
            CharacterOverworld cow;
            Plugin.Logger.LogWarning("testSetStatusIcons");
            GameObject immunities = __instance.transform.Find("immunities").gameObject;
            GameObject ailments = __instance.transform.Find("aliments").gameObject;
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetNewCurseRPC))]
        [HarmonyPostfix]
        static void NewCurse()
        {
            Plugin.Logger.LogWarning("testNewCurse");
        }

        static void Test(CharacterOverworld cow)
        {
            foreach (CharacterStats.CurseType curse in cow.m_CharacterStats.m_ActiveCurses)
            {
                
            }
        }

        // game objects
        readonly Dictionary<string, string> effects = new()
        {
            {"ambushImmunity", "Ambush Immunity"},
            {"bleedImmunity", "Bleed Immunity"},
            {"confuseImmunity", "Confuse Immunity"},
            {"curseImmunity", "Curse Immunity"},
            {"deathmarkImmunity", "Deathmark Immunity"},
            {"fireImmunity", "Fire Immunity"},
            {"freezeImmunity", "Freeze Immunity"},
            {"poisonImmunity", "Poison Immunity"},
            {"scareImmunity", "Scare Immunity"},
            {"shockImmunity", "Shock Immunity"},
            {"stealImmunity", "Steal Immunity"},
            {"stunImmunity", "Stun Immunity"},
            {"acidImmunity", "Acid Immunity"},
            {"wetImmunity", "Wet Immunity"},
            {"diseaseImmunity", "Disease Immunity"},
            {"petrifyImmunity", "Petrify Immunity"},
            {"lightFooted", "Light Footed"},
            {"resistDeath", "Resist Death"},
            {"curseUnlucky", "Curse Unlucky"},
            {"curseFeeble", "Curse Feeble"},
            {"curseBlind", "Curse Blind"},
            {"curseLethargic", "Curse Lethargic"},
            {"curseFoolish", "Curse Lethargic"},
            {"curseUnwell", "Curse Lethargic"},
            {"poisonIconLvl1", "Curse Lethargic"},
            {"poisonIconLvl2", "Curse Lethargic"},
            {"poisonIconLvl3", "Curse Lethargic"},
            {"confused", "Curse Lethargic"},
            {"burning", "Curse Lethargic"},
            {"shocked", "Curse Lethargic"},
            {"stunned", "Curse Lethargic"},
            {"frozen", "Curse Lethargic"},
            {"deathMark", "Curse Lethargic"},
            {"bleeding", "Curse Lethargic"},
            {"attackUp", "Curse Lethargic"},
            {"attackDown", "Curse Lethargic"},
            {"resistUp", "Curse Lethargic"},
            {"resistDown", "Curse Lethargic"},
            {"armorUp", "Curse Lethargic"},
            {"armorDown", "Curse Lethargic"},
            {"evadeUp", "Curse Lethargic"},
            {"evadeDown", "Curse Lethargic"},
            {"speedUp", "Curse Lethargic"},
            {"speedDown", "Curse Lethargic"},
            {"taunt", "Curse Lethargic"},
            {"acid", "Curse Lethargic"},
            {"entangled", "Curse Lethargic"},
            {"damageReflect", "Curse Lethargic"},
            {"protect", "Curse Lethargic"},
            {"[1] groupShield", "Curse Lethargic"},
            {"reflect", "Curse Lethargic"},
            {"wet", "Curse Lethargic"},
            {"[1] disease", "Curse Lethargic"},
            {"petrified", "Curse Lethargic"},
        };
    }
}