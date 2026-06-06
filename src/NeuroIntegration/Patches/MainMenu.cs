using HarmonyLib;
using UnityEngine;
using StartGameFE;
using System.Collections;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration.Actions;
using GridEditor;
using UnityEngine.Assertions;

namespace Pyran.NeuroFTK.NeuroIntegration;

[HarmonyPatch]
public class MainMenu
{
    public static uiFTKButton newGameBtn;
    public static uiFTKButton resumeBtn;
    public static uiFTKButton loreBtn;

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
    }

    static IEnumerator DelayMainMenuAction(MainScreen instance)
    {
        yield return new WaitForSeconds(1.0f);
        newGameBtn = instance.transform.Find("ButtonRoot/New")?.GetChild(0)?.GetComponent<uiFTKButton>();
        resumeBtn = instance.transform.Find("ButtonRoot/Resume")?.GetChild(0)?.GetComponent<uiFTKButton>();
        loreBtn = instance.transform.Find("ButtonRoot/Lore")?.GetChild(0)?.GetComponent<uiFTKButton>();
        bool purchase = HasPurchasableLore();
        Plugin.Logger.LogMessage($"resume button is {resumeBtn.isActiveAndEnabled}");
        ActionWindow window = ActionWindow.Create(instance.gameObject);
        // window.SetForce(3, "Begin the game or spend lore points if you can afford anything", "the games main menu", true, ActionsForce.Priority.Low);
        window.AddAction(new MainMenuAction(instance, resumeBtn.isActiveAndEnabled, purchase));
        UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
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
            if (!loreItem.CanAfford()) continue;
            if (loreItem.IsPurchased()) continue;
            if (!loreItem.IsRevealed()) continue;
            // skip dlc for now?
            if (loreItem.m_DLC != FTK_dlc.ID.None) continue;
            purchasableItemsCount += 1;
        }
        return purchasableItemsCount;
    }

    public static IEnumerator SelectButtonWithDelay(uiFTKButton button, float wait = 1.0f)
    {
        if (button == null) yield break;
        if (!button.isActiveAndEnabled)
        {
            Plugin.Logger.LogWarning($"button {button.name} is disabled");
            yield break;
        }
        button.OnPointerEnter(null);
        yield return new WaitForSeconds(wait);
        Assert.IsNotNull(button);
        button?.OnControllerClick();
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
