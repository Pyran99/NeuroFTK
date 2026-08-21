using System;
using System.Collections;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.Utils;
using UnityEngine;


namespace Pyran.NeuroFTK.NeuroIntegration.ContextEvents
{
    [HarmonyPatch]
    public class PortraitMessage
    {
        static ActionWindow activeWindow;

        // npc talking
        [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.InitializeMessage))]
        [HarmonyPatch([typeof(FTK_talkingHead), typeof(string), typeof(Action), typeof(float), typeof(bool), typeof(bool), typeof(GameObject), typeof(string)])]
        [HarmonyPostfix]
        static void NPCTalking(uiPortraitMessageHud __instance, string _message)
        {
            Plugin.Logger.LogMessage("npc talking");
            string msg = StringReplace.RemoveStyling(_message);
            Context.Send(StringMessages.PortraitMsg.Format([__instance.m_Speaker.text, __instance.m_SpeakerTitle.text, msg]));
        }

        // user character talking
        [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.InitializeMessage))]
        [HarmonyPatch([typeof(UserNPC), typeof(string), typeof(Action), typeof(float), typeof(bool), typeof(bool), typeof(GameObject), typeof(string)])]
        [HarmonyPostfix]
        static void UserTalking(uiPortraitMessageHud __instance, string _message)
        {
            Plugin.Logger.LogMessage("user talking");
            string msg = StringReplace.RemoveStyling(_message);
            Context.Send(StringMessages.PortraitMsg.Format([__instance.m_Speaker.text, __instance.m_SpeakerTitle.text, msg]));
        }

        [HarmonyPatch(typeof(uiPortraitMessageHud), "ActivateMessagePanel")]
        [HarmonyPostfix]
        static IEnumerator PanelShown(IEnumerator __result, uiPortraitMessageHud __instance, float _delay)
        {
            while (__result.MoveNext()) yield return __result.Current;
            QuickTimerCallback timer = new(() => ContinueAfterMessageSent(__instance), __instance.m_MessagePanel.gameObject, _delay*1000f);
        }

        [HarmonyPatch(typeof(FTKClickAnywhere), nameof(FTKClickAnywhere.OnClose))]
        [HarmonyPostfix]
        static void MessageClosed()
        {
            UnityEngine.Object.Destroy(activeWindow);
        }

        // // this may not always be called
        // [HarmonyPatch(typeof(uiPortraitMessageHud), nameof(uiPortraitMessageHud.ZoomInFinished))]
        // [HarmonyPostfix]
        // static void AfterZoom(uiPortraitMessageHud __instance)
        // {
        //     QuickTimerCallback timer = new(() => ContinueAfterMessageSent(__instance), __instance.m_MessagePanel.gameObject, 2000f);
        // }

        // this could be used to grab quest data => is set to m_Quest
        [HarmonyPatch(typeof(MessageCoordinator), nameof(MessageCoordinator.ShowQuestMessage))]
        [HarmonyPostfix]
        static void AfterShowQuestMessage(object[] _arguments)
        {
            Plugin.Logger.LogMessage("show quest message"); // 2
            int num = (int)_arguments[0];
            QuestLogicBase quest = GameLogic.Instance.GetQuestByID(num);
            _ = quest.m_Destination;
            // Plugin.Logger.LogMessage($"{quest.m_Destination.m_BigIndex} - {quest.m_Destination.m_SmallIndex}"); // 13 - 18
            // quest.CheckIsComplete();
            // quest.DetermineDestinations();
            // quest.GetCurrentDestinationLocation();
            // quest.GetLocalizedOneLineDesc();
            //MessagePresenter.Instance.PresentMessage(_mi.m_ID, closeFunc, closePartFunc, questByID, questMessageType, flag);
            //base.StartCoroutine(this.WaitPortraitToClose(_msgInstanceID, _messageCloseCallback, _msgPartCloseCB, _quest, _questMsgType, _enableButton));
        }

        static void ContinueAfterMessageSent(uiPortraitMessageHud instance)
        {
            if (activeWindow != null) return;
            activeWindow = ContinueMessageHudAction.RegisterAction(instance.gameObject);
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