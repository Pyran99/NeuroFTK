using System;
using System.Collections;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;


namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents
{
    [HarmonyPatch]
    public class PortraitMessage
    {
        static bool doOnce = false;
        static ActionWindow activeWindow;

        // npc talking
        [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.InitializeMessage))]
        [HarmonyPatch([typeof(FTK_talkingHead), typeof(string), typeof(Action), typeof(float), typeof(bool), typeof(bool), typeof(GameObject), typeof(string)])]
        [HarmonyPostfix]
        static void NPCTalking(uiPortraitMessageHud __instance, string _message)
        {
            Plugin.Logger.LogMessage("npc talking");
            string msg = StringReplace.RemoveStyling(_message);
            Context.Send($"{__instance.m_Speaker.text} ({__instance.m_SpeakerTitle.text}) says: {msg}");
            ContinueAfterMessageSent(__instance);
        }

        // user character talking
        [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.InitializeMessage))]
        [HarmonyPatch([typeof(UserNPC), typeof(string), typeof(Action), typeof(float), typeof(bool), typeof(bool), typeof(GameObject), typeof(string)])]
        [HarmonyPostfix]
        static void UserTalking(uiPortraitMessageHud __instance, string _message)
        {
            Plugin.Logger.LogMessage("user talking");
            string msg = StringReplace.RemoveStyling(_message);
            Context.Send($"{__instance.m_Speaker.text} ({__instance.m_SpeakerTitle.text}) says: {msg}");
            ContinueAfterMessageSent(__instance);
        }

        static void ContinueAfterMessageSent(uiPortraitMessageHud instance)
        {
            if (activeWindow != null) return;
            doOnce = false;
            activeWindow = ContinueMessageHud.RegisterAction(instance.gameObject);
            // instance.StartCoroutine(Continue(instance));
        }

        static IEnumerator Continue(uiPortraitMessageHud instance)
        {
            if (doOnce) yield break;
            doOnce = true;
            yield return new WaitForSeconds(6f);
            // this holds an event to current click anywhere that would call its close
            // FTKClickAnywhere.Instance.OnClick();
            Plugin.Logger.LogMessage("portrait click continue");
            doOnce = false;
        }

        // this may not always be called
        [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.ZoomInFinished))]
        [HarmonyPostfix]
        static void AfterZoom(uiPortraitMessageHud __instance)
        {
            Plugin.Logger.LogMessage("portrait after zoom in");
            ContinueAfterMessageSent(__instance);
        }

        [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.UseOkayButton))]
        [HarmonyPostfix]
        static void OnClickToContinue()
        {
            Plugin.Logger.LogMessage("continue btn pressed");
        }

        // this could be used to grab quest data => is set to m_Quest
        [HarmonyPatch(typeof(MessageCoordinator), nameof(MessageCoordinator.ShowQuestMessage))]
        [HarmonyPostfix]
        static void AfterShowQuestMessage(object[] _arguments)
        {
            Plugin.Logger.LogMessage("show quest message"); // 2
            int num = (int)_arguments[0];
            QuestLogicBase quest = GameLogic.Instance.GetQuestByID(num);
            _ = quest.m_Destination;
            Plugin.Logger.LogMessage($"{quest.m_Destination.m_BigIndex} - {quest.m_Destination.m_SmallIndex}"); // 13 - 18
            // quest.CheckIsComplete();
            // quest.DetermineDestinations();
            // quest.GetCurrentDestinationLocation();
            // quest.GetLocalizedOneLineDesc();
            //MessagePresenter.Instance.PresentMessage(_mi.m_ID, closeFunc, closePartFunc, questByID, questMessageType, flag);
            //base.StartCoroutine(this.WaitPortraitToClose(_msgInstanceID, _messageCloseCallback, _msgPartCloseCB, _quest, _questMsgType, _enableButton));
        }

        [HarmonyPatch(typeof(FTKUI), nameof(FTKUI.EnableGlobalMessage))]
        [HarmonyPostfix]
        static void AfterEnableGlobalMessage(string _message)
        {
            Plugin.Logger.LogMessage("enable global message");
            Plugin.Logger.LogMessage(_message);
        }
    }
}


        // // quest message
        // [HarmonyPatch(typeof(MessagePresenter), "WaitPortraitToClose")]
        // [HarmonyPatch([typeof(int),typeof(Action),typeof(Action<ContinueFSM, int>),typeof(QuestLogicBase),typeof(GameEventManager.QuestMessageType),typeof(bool)])]
        // [HarmonyPostfix]
        // static void PortraitClosed()
        // {
        //     Plugin.Logger.LogMessage("portrait close quest message type"); // 1
        // }

        // // engage message
        // [HarmonyPatch(typeof(MessagePresenter), "WaitPortraitToClose")]
        // [HarmonyPatch([typeof(int),typeof(Action),typeof(Action<ContinueFSM, int>),typeof(MessageCoordinator.EngageMessageType),typeof(int),typeof(bool)])]
        // [HarmonyPostfix]
        // static void PortraitClosed2()
        // {
        //     Plugin.Logger.LogMessage("portrait close engage message type");
        //     // public enum EngageMessageType
        //     // {
        //     // 	EnemySet,
        //     // 	DungeonMiniEncounterStart,
        //     // 	DungeonMiniEncounterEnd,
        //     // 	MessageID,
        //     // 	SessionDialogue
        //     // }
        // }