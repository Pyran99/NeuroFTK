using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
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
            QuickTimerCallback timer = new(CreateAction, uiTownServiceMenu.Instance.m_DisplayRoot.gameObject);
        }

        [HarmonyPatch(typeof(GeneralMenuBase), nameof(GeneralMenuBase.DisableMenu))]
        [HarmonyPrefix]
        static void ServiceClosed2(GeneralMenuBase __instance)
        {
            if (__instance == uiTownServiceMenu.Instance)
            {
                neuroData.Clear();
                Object.Destroy(window);
            }
        }

        static void CreateAction()
        {
            string ctx = "";
            CharacterOverworld cow = CharacterData.GetActiveCow();
            foreach (uiTownServiceMenu.ServiceButton btn in uiTownServiceMenu.Instance.m_ServiceButtons)
            {
                if (!btn.m_RectTransform.gameObject.activeInHierarchy) continue;
                int cost = int.Parse(btn.m_CostText.text);
                if ((bool)(btn.m_CostText?.gameObject.activeInHierarchy))
                {
                    if (!cow.m_CharacterStats.CanAfford(cost)) continue;
                }
                if (btn.m_ServiceType == MiniHexServiceType.BoatRepair)
                {
                    List<MiniHexBoat> boats = cow.m_HexLand.GetPOI()?.GetNearbyRepairBoats();
                    if (boats.Count == 0)
                    {
                        ctx += "(no boats to repair)\n";
                        continue;
                    }
                }
                else if (btn.m_ServiceType == MiniHexServiceType.BoatReclaim)
                {
                    List<MiniHexBoat> boats = cow.m_HexLand.GetPOI()?.GetNearbyBoats();
                    if (boats.Count == 0)
                    {
                        ctx += "(no boats to reclaim)\n";
                        continue;
                    }
                }
                string _name = btn.m_RectTransform.Find("Text").GetComponent<Text>().text;
                string _desc = GameDescriptions.TownServices[btn.m_ServiceType];
                string _cost = btn.m_CostText.gameObject.activeInHierarchy ? btn.m_CostText.text : "0";
                ctx += $"[{_name}] cost {_cost}. {_desc}\n";
                neuroData.Add(_name, btn.m_RectTransform.GetComponent<uiFTKButton>());
            }
            window = ActionWindow.Create(uiTownServiceMenu.Instance.m_DisplayRoot.gameObject);
            if (neuroData.Count == 0) Context.Send("there are no services you can afford");
            else window.AddAction(new TownServiceAction(neuroData));
            CancelAction cancel = new(window, "close the service window");
            cancel.OnCancelled += CloseServiceWindow;
            window.AddAction(cancel);
            ctx += $"\n(You have {cow.m_CharacterStats.m_Gold} gold)";
            window.SetContext(ctx);
            window.SetForce(0, "choose a service to purchase or close the window", "you are at a town and a service menu has opened", true);
            window.Register();
            UnregisterDisabledObject.QuickCreate(uiTownServiceMenu.Instance.m_DisplayRoot.gameObject, window);
        }

        public static void CloseServiceWindow(ActionWindow _window)
        {
            uiTownServiceMenu.Instance.m_Cancel?.Invoke();
            Object.Destroy(_window);
        }


    }
}