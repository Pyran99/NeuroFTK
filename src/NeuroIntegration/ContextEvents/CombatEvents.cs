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

        public static void PlayerHealthChange(CharacterOverworld character, int change)
        {
            Plugin.Logger.LogWarning("dummy: " + character.GetCurrentDummy());
            if (change > 0)
            {
                dmgTakenString.AppendLine($"{character.m_CharacterStats.m_CharacterName} took {change} damage (health {character.m_CharacterStats.GetHealthDisplayString()})");
            }
            else if (change < 0)
            {
                dmgTakenString.AppendLine($"{character.m_CharacterStats.m_CharacterName} healed {change} (health {character.m_CharacterStats.GetHealthDisplayString()})");
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