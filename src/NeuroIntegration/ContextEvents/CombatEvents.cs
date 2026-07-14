using System.Collections;
using System.Text;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public class CombatEvents
    {
        static StringBuilder dmgTakenString = new();
        static bool waiting = false;

        public static void OnDamageTaken(CharacterOverworld character, int damage, int currentHealth, int maxHealth)
        {
            Plugin.Logger.LogWarning("dummy: " + character.GetCurrentDummy());
            if (damage > 0)
            {
                dmgTakenString.AppendLine($"{character.m_CharacterStats.m_CharacterName} took {damage} damage (health: {currentHealth}/{maxHealth})");
            }
            else if (damage < 0)
            {
                dmgTakenString.AppendLine($"{character.m_CharacterStats.m_CharacterName} healed {damage} (health: {currentHealth}/{maxHealth})");
            }
            if (waiting) return;
            GameLogic.Instance.StartCoroutine(Wait());
        }

        static IEnumerator Wait()
        {
            waiting = true;
            yield return new WaitForEndOfFrame();
            Context.Send(dmgTakenString.ToString());
            dmgTakenString = new();
            waiting = false;
        }

        public static void UnitDied()
        {
            
        }
    }
}