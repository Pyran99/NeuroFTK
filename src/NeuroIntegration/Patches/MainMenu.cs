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
    // When the main menu is shown
    [HarmonyPatch(typeof(MainScreen), nameof(MainScreen.OnSetFocus))]
    [HarmonyPostfix]
    static void OnSetFocus(MainScreen __instance)
    {
        ActionWindow activeWindow = __instance.GetComponent<ActionWindow>();
        if (activeWindow != null)
        {
            Object.Destroy(activeWindow);
        }
        __instance.StartCoroutine(DelayMainMenuAction(__instance));
    }

    static IEnumerator DelayMainMenuAction(MainScreen instance)
    {
        bool purchase = HasPurchasableLore();
        yield return new WaitForSeconds(0.5f);
        ActionWindow window = ActionWindow.Create(instance.gameObject);
        window.SetForce(3, "Actions for the main menu", "", true, ActionsForce.Priority.Low);
        window.AddAction(new MainMenuAction(instance, instance.m_ResumeButton.GetComponent<uiFTKButton>(), purchase));
        UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
        window.Register();
    }

    static bool HasPurchasableLore()
    {
        int lorePoints = LorePersistence.Instance.GetLore();
        Context.Send($"You have {lorePoints} lore points. These can be used in the lore store to unlock new events, characters, equipment, and cosmetics, if you have enough points, or saved for another time.");
        int purchasableItemsCount = 0;
        foreach (FTK_loreItem loreItem in FTK_loreItemDB.GetDB().m_Array)
        {
            if (!loreItem.CanAfford()) continue;
            if (loreItem.IsPurchased()) continue;
            if (!loreItem.IsRevealed()) continue;
            purchasableItemsCount += 1;
        }
        return purchasableItemsCount > 0;
    }
}
