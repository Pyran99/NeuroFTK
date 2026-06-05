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
    // When the main menu is active
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
        // GenerateTestAction(__instance.gameObject);
    }

    static IEnumerator DelayMainMenuAction(MainScreen instance)
    {
        yield return new WaitForSeconds(0.5f);
        bool purchase = HasPurchasableLore();
        ActionWindow window = ActionWindow.Create(instance.gameObject);
        window.SetForce(3, "Begin the game or spend lore points if you can afford anything", "the games main menu", true, ActionsForce.Priority.Low);
        window.AddAction(new MainMenuAction(instance, instance.m_ResumeButton.GetComponent<uiFTKButton>(), purchase, "menu_action_1"));
        window.AddAction(new MainMenuAction(instance, null, false, "menu_action_2")); // testing window
        window.AddAction(new MainMenuAction(instance, null, true, "menu_action_3"));
        UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
        window.Register();
    }

    static public bool HasPurchasableLore()
    {
        int lorePoints = LorePersistence.Instance.GetLore();
        int purchasableItemsCount = 0;
        foreach (FTK_loreItem loreItem in FTK_loreItemDB.GetDB().m_Array)
        {
            if (!loreItem.CanAfford()) continue;
            if (loreItem.IsPurchased()) continue;
            if (!loreItem.IsRevealed()) continue;
            if (loreItem.m_DLC != FTK_dlc.ID.None) continue;
            purchasableItemsCount += 1;
        }
        Context.Send($"You have {lorePoints} lore points and there are {purchasableItemsCount} items you can afford. These can be used in the lore store to unlock new events, characters, equipment, and cosmetics, if you have enough points, or saved for another time.");
        return purchasableItemsCount > 0;
    }

    static void GenerateTestAction(GameObject owner)
    {
        ActionWindow window = ActionWindow.Create(owner);
        window.AddAction(new TestAction());
        window.SetContext("This is a test action window send context");
        window.SetForce(5, "", "", true, ActionsForce.Priority.Low);
        window.Register();
    }
}
