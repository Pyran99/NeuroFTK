using HarmonyLib;
using UnityEngine;
using StartGameFE;
using System.Collections;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
using GridEditor;

namespace Pyran.NeuroFTK.NeuroIntegration;

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
    }

    static IEnumerator DelayMainMenuAction(MainScreen instance)
    {
        yield return new WaitForSeconds(1.0f);
        newGameBtn = instance.transform.Find("ButtonRoot/New")?.GetChild(0)?.GetComponent<uiFTKButton>();
        resumeBtn = instance.transform.Find("ButtonRoot/Resume")?.GetChild(0)?.GetComponent<uiFTKButton>();
        loreBtn = instance.transform.Find("ButtonRoot/Lore")?.GetChild(0)?.GetComponent<uiFTKButton>();
        bool purchase = HasPurchasableLore();
        ActionWindow window = ActionWindow.Create(instance.gameObject);
        window.SetContext("you are in the main menu");
        window.AddAction(new MainMenuAction(instance, resumeBtn.isActiveAndEnabled, purchase));
        window.SetForce(5, "Begin the game or spend lore points if you can afford anything", "the games main menu", true, ActionsForce.Priority.Low);
        UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
        activeWindow = window;
        window.Register();
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
}
