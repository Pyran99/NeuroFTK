using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    /*
    send current characters class info context to neuro
    randomize for new classes
    when finished choose 3 names
    start game
    */
    [HarmonyPatch]
    public class SetupParty
    {
        static bool shownOnce = false;
        static Dictionary<string, Dictionary<string, object>> characters = [];
        static List<uiQuickPlayerCreate> players;
        static uiCharacterCreateRoot characterCreateRoot;


        [HarmonyPatch(typeof(uiCharacterCreateRoot), nameof(uiCharacterCreateRoot.Show))]
        [HarmonyPostfix]
        static void OnPartyScreenShown(uiCharacterCreateRoot __instance)
        {
            shownOnce = false;
            characterCreateRoot = __instance;
            Plugin.Logger.LogMessage("character create scene shown");
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
            Plugin.Logger.LogMessage("after random party");
            SendPartyDetails();
        }


        static void OnPartyVisible()
        {
            Plugin.Logger.LogMessage("party screen visible ready");
            SendPartyDetails();
            ChoosePartyNames.RegisterAction(characterCreateRoot.gameObject);
        }

        static void SendPartyDetails()
        {
            Context.Send("NYI party details");
            Plugin.Logger.LogMessage(string.Join(", ", [.. GetCharacterNames()]));
            Plugin.Logger.LogMessage(string.Join(", ", [.. GetCharacterClasses()]));
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

        static string GetClassesDescriptions()
        {
            string data = "";
            foreach (KeyValuePair<string, Dictionary<string, object>> character in characters)
            {
                data += $"name:'{character.Value["name"]}', description: {character.Value["description"]};\n";
            }
            return data;
            // return data.Split('\n');
        }

        static void ActionStartGame(uiStartGame _instance)
        {
            Plugin.Logger.LogMessage("start game action " + nameof(_instance.EnterGame));
            // _instance.EnterGame();
        }

        public static void NeuroSetCharacterNames(List<string> names)
        {
            characterCreateRoot.StartCoroutine(ChangeNames(names));
        }

        static IEnumerator ChangeNames(List<string> names)
        {
            if (names.Count < 3)
            {
                Context.Send("not enough names");
                yield break;
            }
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
            Plugin.Logger.LogMessage(msg);
            Context.Send(msg);
            Plugin.Logger.LogMessage(string.Join(", ", [.. GetCharacterNames()]));
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


        // [HarmonyPatch(typeof(uiStartGame), "CreateAllCreatePlayerUIs")]
        // [HarmonyPostfix]
        // static void PH3()
        // {
        //     Plugin.Logger.LogMessage("after create UIs"); // 2
        // }

        // [HarmonyPatch(typeof(uiStartGame), nameof(uiStartGame.ShowCreateCharacter))]
        // [HarmonyPostfix]
        // static void PH()
        // {
        //     Plugin.Logger.LogMessage("after show create character"); // 3
        // }

        // [HarmonyPatch(typeof(uiStartGame), "WaitUntilPanningFinished")]
        // [HarmonyPostfix]
        // static void PH2()
        // {
        //     Plugin.Logger.LogMessage("after panning finished"); // 4 => called when panning starts
        // }

