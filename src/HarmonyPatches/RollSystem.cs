using System.Collections;
using HarmonyLib;
using HutongGames.PlayMaker;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class RollSystem
    {
        public static CharacterOverworld currentCOW;
        public static int rollCount;

        [HarmonyPatch(typeof(SlotControl), nameof(SlotControl.SetSlotResults))]
        [HarmonyPrefix]
        static void RollResults(SlotControl __instance, FTKPlayerID _player, string[] _slotResults)
        {
            int success = 0;
            foreach (string result in _slotResults)
            {
                if (IsFailedRoll(result)) continue;
                success++;
            }
            rollCount = success;
            string ctx = "";
            CharacterOverworld cow = FTKHub.Instance.GetCharacterOverworldByFID(_player);
            CharacterDummy dummy = GetDummy(cow, __instance);

            if (__instance.gameObject.name == "enemySlotSystem")
            {
                if (dummy is EnemyDummy)
                {
                    ctx = $"{(dummy as EnemyDummy).m_EnemyCombat.GetEnemyDisplay()} rolled {success}/{_slotResults.Length}";
                }
            }
            else // playerSlotSystem or MovementSlots
            {
                ctx = $"{dummy.m_CharacterOverworld.m_CharacterStats.m_CharacterName} rolled {success}/{_slotResults.Length}";
            }
            Context.Send(ctx);
        }

        // enumerator to show the rolled values
        [HarmonyPatch(typeof(SlotControl), "DisplaySlots")]
        [HarmonyPostfix]
        static IEnumerator SlotResults(IEnumerator __result)
        {
            OverworldFlow.isSearching = false;
            while (__result.MoveNext()) yield return __result.Current;
            // combat rolls are after choice
            if (GameStates.mode == uiGameTrackerHUD.GameTrackerMode.Overworld)
            {
            }
        }

        static CharacterDummy GetDummy(CharacterOverworld _cow, SlotControl _slots)
        {
            if (FTKUI.Instance.m_EnemySlots == _slots)
            {
                return (EnemyDummy)FsmVariables.GlobalVariables.GetFsmObject("compEnemyDummy").Value;
            }
            return _cow.m_CurrentDummy;
        }

        public static bool IsFailedRoll(string slot)
        {
            return slot switch
            {
                "miss" or "vexxed" or "distract" or "badweather" or "scourgeCslot" => true,
                _ => false,
            };
        }

        // // when the characters actions points change. This occurs with each tile passed
        // [HarmonyPatch(typeof(CharacterOverworld), nameof(CharacterOverworld.UpdatePlayerAction))]
        // [HarmonyPostfix]
        // static void UpdatePlayerAction(CharacterOverworld __instance)
        // {
        //     // Plugin.Logger.LogMessage("update player action");
        // }
    }
}