using HarmonyLib;
using UnityEngine;
using StartGameFE;
using System.Collections;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using GridEditor;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.GameConfigs;

namespace Pyran.NeuroFTK.HarmonyPatches;

[HarmonyPatch]
public class MainMenu
{
    public static uiFTKButton newGameBtn;
    public static uiFTKButton resumeBtn;
    public static uiFTKButton loreBtn;
    static ActionWindow activeWindow;

    // When the main menu is active
    [HarmonyPatch(typeof(MainScreen), nameof(MainScreen.OnSetFocus))]
    [HarmonyPostfix]
    static void OnSetFocus(MainScreen __instance)
    {
        __instance.StartCoroutine(DelayMainMenuAction(__instance));
        GlobalConfig.gameInitialized = false;
    }

    static IEnumerator DelayMainMenuAction(MainScreen instance)
    {
        yield return new WaitForSeconds(1.0f);
        FindButtons(instance);
        bool purchase = HasPurchasableLore();
        activeWindow = MainMenuAction.RegisterAction(instance, resumeBtn?.isActiveAndEnabled ?? false, purchase);
        UnregisterDisabledObject.QuickCreate(instance.gameObject, activeWindow);
    }

    static void FindButtons(MainScreen instance)
    {
        newGameBtn = instance.transform.Find("ButtonRoot/New")?.GetChild(0)?.GetComponent<uiFTKButton>();
        resumeBtn = instance.transform.Find("ButtonRoot/Resume")?.GetChild(0)?.GetComponent<uiFTKButton>();
        loreBtn = instance.transform.Find("ButtonRoot/Lore")?.GetChild(0)?.GetComponent<uiFTKButton>();
    }

    public static bool HasPurchasableLore()
    {
        int lorePoints = LorePersistence.Instance.GetLore();
        int purchasableItemsCount = GetPurchasableLoreCount();
        Context.Send($"You have {lorePoints} lore points and there are {purchasableItemsCount} items you can afford. These can be used in the lore store to unlock new events, characters, equipment, and cosmetics, if you have enough points, or saved for another time.");
        return purchasableItemsCount > 0;
    }

    public static int GetPurchasableLoreCount()
    {
        int purchasableItemsCount = 0;
        foreach (FTK_loreItem loreItem in FTK_loreItemDB.GetDB().m_Array)
        {
            if (!CanPurchaseItem(loreItem)) continue;
            purchasableItemsCount += 1;
        }
        return purchasableItemsCount;
    }

    public static bool CanPurchaseItem(FTK_loreItem item)
    {
        if (!item.CanAfford()) return false;
        if (item.IsPurchased()) return false;
        if (!item.IsRevealed()) return false; // dlc is also checked here
        return true;
    }

    public static void NeuroDecision(string decision)
    {
        switch (decision)
        {
                case "new game":
                    SelectedButton(newGameBtn);
                    break;
                case "resume game":
                    SelectedButton(resumeBtn);
                    break;
                case "spend lore":
                    SelectedButton(loreBtn);
                    break;
                default:
                    Plugin.Logger.LogError($"invalid main menu action '{decision}'");
                    break;
        }
    }

    static void SelectedButton(uiFTKButton button)
    {
        SelectButton.StartCoroutine(button, 1.0f);
    }
}
