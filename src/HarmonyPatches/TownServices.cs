using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class TownServices
    {
        static ActionWindow window;
        static Dictionary<string, uiFTKButton> neuroData = [];

        [HarmonyPatch(typeof(uiTownServiceMenu), nameof(uiTownServiceMenu.RefreshServicesButtons))]
        [HarmonyPostfix]
        static void ServiceOpened()
        {
            // List<MiniHexServiceType> services = _hex.GetServices();
            neuroData.Clear();
            Object.Destroy(window);
            Plugin.Logger.LogWarning("services enabled");
            QuickTimerCallback timer = new(CreateAction, uiTownServiceMenu.Instance.m_DisplayRoot.gameObject);
        }

        [HarmonyPatch(typeof(GeneralMenuBase), nameof(GeneralMenuBase.DisableMenu))]
        [HarmonyPrefix]
        static void ServiceClosed2(GeneralMenuBase __instance)
        {
            Plugin.Logger.LogWarning("services disabled");
            if (__instance == uiTownServiceMenu.Instance)
            {
                neuroData.Clear();
                Plugin.Logger.LogWarning("service type instance");
                Object.Destroy(window);
            }
        }

        static void CreateAction()
        {
            string ctx = "";
            foreach (uiTownServiceMenu.ServiceButton btn in uiTownServiceMenu.Instance.m_ServiceButtons)
            {
                if (!btn.m_RectTransform.gameObject.activeInHierarchy) continue;
                int cost = int.Parse(btn.m_CostText.text);
                if (GameLogic.Instance.GetCurrentCOW().m_CharacterStats.CanAfford(cost)) continue;
                string _name = btn.m_RectTransform.Find("Text").GetComponent<Text>().text;
                string _desc = GameDescriptions.TownServices[btn.m_ServiceType];
                string _cost = btn.m_CostText.text;
                Plugin.Logger.LogMessage($"{_name} - {_desc} - {_cost}");
                ctx += $"[{_name}] cost {_cost}. {_desc}\n";
                neuroData.Add(_name, btn.m_RectTransform.GetComponent<uiFTKButton>());
            }
            window = ActionWindow.Create(uiTownServiceMenu.Instance.gameObject);
            window.AddAction(new TownServiceAction(neuroData));
            window.SetContext(ctx);
            window.SetForce(2, "choose a service to perform", "you are at a town and a service menu has opened");
            window.Register();
        }

        static uiTownServiceMenu.ServiceButton GetServiceButton(MiniHexServiceType type)
        {
            foreach (uiTownServiceMenu.ServiceButton btn in uiTownServiceMenu.Instance.m_ServiceButtons)
            {
                if (type == btn.m_ServiceType)
                {
                    return btn;
                }
            }
            return null;
        }

    }
}