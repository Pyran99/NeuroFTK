using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class TownQuestBoard
    {
        static ActionWindow window;
        static Dictionary<string, uiFTKButton> neuroData = [];
        static List<QuestListItem> items = [];


        [HarmonyPatch(typeof(uiGetQuestMenu), nameof(uiGetQuestMenu.EnableMenu))]
        [HarmonyPostfix]
        static void BoardOpened(MiniHexInfo _hex)
        {
            Plugin.Logger.LogWarning("quest board open");
            // MiniHexTown hexTown = (MiniHexTown)_hex;
            // if (hexTown.m_QuestList.Count == 0)
            // {
            //     Plugin.Logger.LogWarning("1 no quests");
            //     return;
            // }
            if (uiGetQuestMenu.Instance.m_NoQuestDisplay.text != string.Empty)
            {
                Plugin.Logger.LogWarning("2 no quests");
                return;
            }

        }

        [HarmonyPatch(typeof(GeneralMenuBase), nameof(GeneralMenuBase.DisableMenu))]
        [HarmonyPrefix]
        static void ServiceClosed2(GeneralMenuBase __instance)
        {
            if (__instance == uiGetQuestMenu.Instance)
            {
                // neuroData.Clear();
                Plugin.Logger.LogWarning("quest type instance close");
                // Object.Destroy(window);
            }
        }


        static void GetQuestItems()
        {
            // uiGetQuestMenu.Instance.m_ListRoot.transform;
            for (int i = 0; i < uiGetQuestMenu.Instance.m_ListRoot.transform.childCount; i++)
            {
                GameObject child = uiGetQuestMenu.Instance.m_ListRoot.transform.GetChild(i).gameObject;
                if (child.GetComponent<QuestListItem>())
                {
                    items.Add(child.GetComponent<QuestListItem>());
                }
            }
        }
        
    }
}