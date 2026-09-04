using System.Collections;
using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using NeuroSdk.Internal;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    /// <summary>
    /// Party setup screen
    /// </summary>
    [HarmonyPatch]
    public class SetupParty
    {
        static bool shownOnce = false;
        static List<uiQuickPlayerCreate> players;
        static uiCharacterCreateRoot characterCreateRoot;
        static readonly float waitTime = 3.0f;


        [HarmonyPatch(typeof(uiCharacterCreateRoot), nameof(uiCharacterCreateRoot.Show))]
        [HarmonyPostfix]
        static void OnPartyScreenShown(uiCharacterCreateRoot __instance)
        {
            shownOnce = false;
            characterCreateRoot = __instance;
        }

        [HarmonyPatch(typeof(uiQuickPlayerCreate), nameof(uiQuickPlayerCreate.Show))]
        [HarmonyPostfix]
        static void AfterCameraPan(uiQuickPlayerCreate __instance)
        {
            if (shownOnce) return; // prevent 3 calls
            shownOnce = true;
            players = [.. characterCreateRoot.m_Players];
            __instance.StartCoroutine(WaitUntilInteractable());
        }

        [HarmonyPatch(typeof(uiCharacterCreateRoot), nameof(uiCharacterCreateRoot.RandomParty))]
        [HarmonyPostfix]
        static void OnPartyRandomized()
        {
            Context.Send("your party has been randomized", true);
            SendPartyDetails();
            characterCreateRoot.StartCoroutine( QuickTimerCallback.WaitRoutine(() => ConfiguePartyAction.RegisterConfigurePartyActions(characterCreateRoot.gameObject), characterCreateRoot.gameObject));
        }

        [HarmonyPatch(typeof(FTKHub), nameof(FTKHub.EnterFahrul))]
        [HarmonyPostfix]
        static void EnteringWorld()
        {
            Context.Send("entering the world of Fahrul", true);
        }

        static void OnPartyVisible()
        {
            if (uiStartGame.Instance.m_IsResuming)
            {
                ActionStartGame();
                return;
            }
            SendPartyDetails();
            characterCreateRoot.StartCoroutine(QuickTimerCallback.WaitRoutine(() => ConfiguePartyAction.RegisterConfigurePartyActions(characterCreateRoot.gameObject), characterCreateRoot.gameObject));
        }

        static void SendPartyDetails(bool addClassData = true)
        {
            string data = "";
            if (addClassData)
            {
                FTK_playerGameStartDB db = FTK_playerGameStartDB.GetDB();
                foreach (uiQuickPlayerCreate player in players)
                {
                    string serialized = Jason.Serialize(CharacterType.SerializeGameClass(db.GetEntry((FTK_playerGameStart.ID)player.m_ClassID)));
                    data += $"- {serialized}\n";
                }
                data = $"## current party classes \n{data} \n";
            }
            List<string> names = GetCharacterNames();
            List<string> classes = GetCharacterClasses();
            data += "## party setup is \n";
            foreach (string name in names)
            {
                data += $"- {name}: {classes[names.IndexOf(name)]} \n";
            }
            Context.Send(data);
        }

// [Message:Neuro For the King] Player 1, Player 2, Player 3
        static List<string> GetCharacterNames()
        {
            List<string> names = [];
            foreach (uiQuickPlayerCreate player in characterCreateRoot.m_Players)
            {
                names.Add(player.m_PlayerNameStr);
            }
            return names;
        }

// [Message:Neuro For the King] Hunter, Minstrel, Hunter
        static List<string> GetCharacterClasses()
        {
            List<string> names = [];
            foreach (uiQuickPlayerCreate player in characterCreateRoot.m_Players)
            {
                names.Add(player.m_PlayerClass.text);
            }
            return names;
        }

        static void ActionStartGame()
        {
            uiFTKButton btn = characterCreateRoot.transform.Find("UIRoot/ButtonRoot/StartButton").GetComponent<uiFTKButton>();
            SelectButton.StartCoroutine(btn, 0.5f);
        }

        public static void NeuroRandomizeParty()
        {
            characterCreateRoot.RandomParty();
        }

        public static void NeuroSetCharacterNames(List<string> names)
        {
            characterCreateRoot.StartCoroutine(ChangeNames(names));
        }

        static IEnumerator ChangeNames(List<string> names)
        {
            foreach (uiQuickPlayerCreate player in players)
            {
                string name = names[players.IndexOf(player)];
                player.m_PlayerNameInput.OnTextButtonClick();
                player.m_PlayerNameInput.OnTextChanged(name);
                player.m_PlayerNameInput.OnEditFinished(name);
                yield return new WaitForSeconds(0.5f);
            }
            string msg = $"changed names to ";
            foreach (string name in names)
            {
                msg += $"'{name}' ";
            }
            SendPartyDetails(false);
            Context.Send(msg);
            Context.Send("tell chat about your party members while the game begins");
            yield return new WaitForSeconds(waitTime);
            ActionStartGame();
        }

        // wait for player canvas to be visible
        static IEnumerator WaitUntilInteractable()
        {
            float startTime = Time.time;
            for (; ; )
            {
                float alpha = Mathf.Clamp01((Time.time - startTime) / VisualParams.Instance.m_CreateCharacterAppearTime);
                if (alpha >= 1.0f)
                {
                    OnPartyVisible();
                    break;
                }
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            shownOnce = false;
        }
    }
}
