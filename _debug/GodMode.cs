using HarmonyLib;
using Pyran.NeuroFTK.GameConfigs;
using UnityEngine;

namespace Pyran.NeuroFTK.ModDebug
{
    [HarmonyPatch]
    public class GodMode
    {

        [HarmonyPatch(typeof(GameStart), "Start")]
        [HarmonyPostfix]
        static void CreateToggleInstance()
        {
            ToggleGodModeEffects.Create();
        }
        
        // effects like burning. calls to SetSpecificHealthRPC
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TakeSecondaryDamageCombat))]
        [HarmonyPrefix]
        static void OnPlayerBeforeReceiveDamage(ref int _dmg)
        {
            Plugin.Logger.LogMessage("second dmg combat");
            if(ToggleGodModeEffects.Instance.godModeType == ToggleGodModeEffects.GodModeType.NO_DMG)
            {
                _dmg = 0;
            }
        }

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TakeSecondaryDamageNonCombat))]
        [HarmonyPrefix]
        static void OnPlayerBeforeReceiveDamageNonCombat(ref int _dmg)
        {
            Plugin.Logger.LogMessage("second dmg non combat");
            if(ToggleGodModeEffects.Instance.godModeType == ToggleGodModeEffects.GodModeType.NO_DMG)
            {
                _dmg = 0;
            }
        }

        // non rpc version calls directly to this as rpc all self
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TakeSecondaryDamageNonCombatRPC))]
        [HarmonyPrefix]
        static void OnPlayerBeforeReceiveDamageNonCombatRPC(ref int _dmg)
        {
            Plugin.Logger.LogMessage("second dmg non combat rpc");
            if(ToggleGodModeEffects.Instance.godModeType == ToggleGodModeEffects.GodModeType.NO_DMG)
            {
                _dmg = 0;
            }
        }

        // only call for taking dmg
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetSpecificHealthRPC))]
        [HarmonyPrefix]
        static void SetSpecificHealthRPC(CharacterStats __instance, ref int _newHp)
        {
            int beforeHealth = __instance.m_HealthCurrent;
            if (_newHp > beforeHealth) return; // heal
            if (ToggleGodModeEffects.Instance.godModeType == ToggleGodModeEffects.GodModeType.NO_DMG)
            {
                _newHp = beforeHealth;
            }
            else if (ToggleGodModeEffects.Instance.godModeType == ToggleGodModeEffects.GodModeType.FULL_HEALTH)
            {
                _newHp = __instance.MaxHealth;
            }
        }

        // mainly for hud text. heal only probably?
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GainSpecificHealth))]
        [HarmonyPrefix]
        static void OnHealthAboutToChange(ref int _hpGain)
        {
            if (ToggleGodModeEffects.Instance.godModeType == ToggleGodModeEffects.GodModeType.NO_DMG)
            {
                if (_hpGain < 0)
                {
                    Plugin.Logger.LogMessage("god mode no dmg");
                    _hpGain = 0;
                }
            }
        }

    }

#region God mode object
    public class ToggleGodModeEffects: MonoBehaviour
    {

        public static void Create()
        {
            if (Instance != null) return;
            var obj = new GameObject("ToggleGodModeEffects");
            DontDestroyOnLoad(obj);
            Instantiate(obj);
            Instance = obj.AddComponent<ToggleGodModeEffects>();
        }
        
        public enum GodModeType
        {
            NONE,
            NO_DMG,
            FULL_HEALTH,
        }
        public GodModeType godModeType = GodModeType.NONE;

        public static ToggleGodModeEffects Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Plugin.Logger.LogError("more than 1 ToggleGodModeEffects");
                Destroy(this);
            }
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                if (!GlobalConfig.debug_mode) return;
                godModeType += 1;
                if (godModeType > GodModeType.FULL_HEALTH) godModeType = GodModeType.NONE;
                GameCheat.Instance.m_IsGodMode = godModeType != GodModeType.NONE;
                Plugin.Logger.LogMessage($"godmode type changed to {godModeType}");
            }
        }
    }
#endregion

}