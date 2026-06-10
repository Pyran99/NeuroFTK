using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration.Actions
{
    /// <summary>
    /// Party setup screen
    /// </summary>
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
            ConfigueParty.RegisterConfigurePartyActions(characterCreateRoot.gameObject);
        }


        static void OnPartyVisible()
        {
            SendPartyDetails();
            ConfigueParty.RegisterConfigurePartyActions(characterCreateRoot.gameObject);
        }

        static void SendPartyDetails()
        {
            // { "Player 1: class: Hunter },
            List<string> names = GetCharacterNames();
            List<string> classes = GetCharacterClasses();
            string data = "your party setup is: ";
            foreach (string name in names)
            {
                data += $"{{ {name}: class: {classes[names.IndexOf(name)]}}}, ";
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

        static string GetClassesDescriptions()
        {
            string data = "";
            foreach (KeyValuePair<string, Dictionary<string, object>> character in characters)
            {
                data += $"name:'{character.Value["name"]}', description: {character.Value["description"]};\n";
            }
            return data;
        }

        static void ActionStartGame()
        {
            uiStartGame instance = uiStartGame.Instance;
            Plugin.Logger.LogMessage("NYI start game action " + nameof(instance.EnterGame));
            // _instance.EnterGame();
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
            Plugin.Logger.LogMessage(msg);
            Context.Send(msg);
            SendPartyDetails();
            Context.Send("tell chat about your party members while the game begins");
            yield return new WaitForSeconds(10f);
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

