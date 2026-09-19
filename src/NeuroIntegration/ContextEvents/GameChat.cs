using System.Collections;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class GameChat
    {
        static string lastNeuroMsg = "";
        static Coroutine routine;

        [HarmonyPatch(typeof(uiChatBox), nameof(uiChatBox.AddMessage))]
        [HarmonyPostfix]
        static void OnChatMsg(string _name, string _msg)
        {
            if (routine != null) uiChatBox.Instance?.StopCoroutine(routine);
            routine = uiChatBox.Instance?.StartCoroutine(ShowMsg());
            if (_msg == lastNeuroMsg)
            {
                lastNeuroMsg = "";
                return;
            }
            Context.Send($"{_name} sent chat message: {_msg}");
        }

        public static void SendChatMsg(string msg = "")
        {
            if (uiChatBox.Instance)
            {
                lastNeuroMsg = msg;
                GameFlow.Instance.ChatMessage(msg);
            }
        }

        static IEnumerator ShowMsg()
        {
            uiChatBox.Instance.OnButtonOpen();
            yield return new WaitForSeconds(5f);
            uiChatBox.Instance.OnButtonClose();
            routine = null;
            // if (uiChatBox.Instance.m_TextBoxRoot.gameObject.activeSelf) uiChatBox.Instance.OnButtonToggle();
        }
    }
}