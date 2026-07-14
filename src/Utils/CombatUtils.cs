using System.Collections.Generic;

namespace Pyran.NeuroFTK.Utils
{
    public static class CombatUtils
    {
        public static CharacterDummy GetDummyInCombat(FTKPlayerID id)
        {
            foreach (KeyValuePair<FTKPlayerID, CharacterDummy> dummy in EncounterSession.Instance.m_Dummies)
            {
                if (dummy.Key == id)
                {
                    return dummy.Value;
                }
            }
            return null;
        }

        public static string GetEnemyName(EnemyDummy dummy)
        {
            if (!uiEnemyHUD.Instance.m_EnemyHudDictionary.ContainsKey(dummy))
            {
                Plugin.Logger.LogError($"invalid dummy ui {dummy?.m_EnemyCombat?.GetEnemyDisplay()}");
                return "";
            }
            uiEachEnemyHud hud = uiEnemyHUD.Instance.m_EnemyHudDictionary[dummy];
            string name = hud.m_EnemyNameDisplay.text;
            return StringReplace.ReplaceNewLineSpace(name);
            
        }
    }
}