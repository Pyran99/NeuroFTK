using System.Collections;
using System.Collections.Generic;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;
using Pyran.NeuroFTK.Utils;
using Pyran.NeuroFTK.NeuroIntegration;
using NeuroSdk.Internal;
using Pyran.NeuroFTK.GameConfigs;
using System.Linq;
using System.Text;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    /// <summary>
    /// Party setup screen
    /// </summary>
    [HarmonyPatch]
    public class SetupParty
    {
        static int partyMemberCount = 0;
        static List<uiQuickPlayerCreate> players;
        static uiCharacterCreateRoot characterCreateRoot;


        [HarmonyPatch(typeof(uiCharacterCreateRoot), nameof(uiCharacterCreateRoot.Show))]
        [HarmonyPostfix]
        static void OnPartyScreenShown(uiCharacterCreateRoot __instance)
        {
            partyMemberCount = 0;
            characterCreateRoot = __instance;
        }

        [HarmonyPatch(typeof(uiStartGame), "WaitUntilPanningFinished")]
        [HarmonyPostfix]
        static IEnumerator PartySetupPanFinished(IEnumerator __result)
        {
            while (__result.MoveNext()) yield return __result.Current;
            yield return null;
            players = [.. characterCreateRoot.m_Players];
            partyMemberCount = players.Count;
            characterCreateRoot.StartCoroutine(WaitUntilInteractable());
        }

        [HarmonyPatch(typeof(uiQuickPlayerCreate), nameof(uiQuickPlayerCreate.Show))]
        [HarmonyPostfix]
        static void OnMemberCreated(uiQuickPlayerCreate __instance)
        {
            players = [.. characterCreateRoot.m_Players]; // updated with instance awake
        }

        [HarmonyPatch(typeof(uiCharacterCreateRoot), nameof(uiCharacterCreateRoot.RandomParty))]
        [HarmonyPostfix]
        static void OnPartyRandomized()
        {
            Context.Send("your party has been randomized", true);
            RegisterWindow(characterCreateRoot);
        }

        [HarmonyPatch(typeof(FTKHub), nameof(FTKHub.EnterFahrul))]
        [HarmonyPostfix]
        static void EnteringWorld()
        {
            Context.Send("entering the world of Fahrul", true);
        }

        static void OnPartyVisible()
        {
            if (Multiplayer.IsMultiplayer())
            {
                //TODO up to neuro create amount
                for (int i = 0; i < (3 - partyMemberCount); i++)
                {
                    if (!characterCreateRoot.m_SelectSlotButton.gameObject.activeSelf) break;
                    characterCreateRoot.OnSelectPlayerSlot();
                }
                players = [.. characterCreateRoot.m_Players];
                partyMemberCount = players.Count;
                RegisterWindow(characterCreateRoot);
            }
            else
            {
                if (uiStartGame.Instance.m_IsResuming)
                {
                    ActionStartGame();
                    return;
                }
                RegisterWindow(characterCreateRoot);
            }
        }

        static void SendPartyDetails(bool addClassData = true)
        {
            StringBuilder sb = new();
            if (addClassData)
            {
                FTK_playerGameStartDB db = FTK_playerGameStartDB.GetDB();
                sb.AppendLine("## current party class info ");
                foreach (uiQuickPlayerCreate player in players)
                {
                    string serialized = Jason.Serialize(CharacterType.SerializeGameClass(db.GetEntry((FTK_playerGameStart.ID)player.m_ClassID)));
                    sb.AppendLine($"- {serialized}");
                }
            }
            sb.AppendLine("## party setup is ");
            for (int i = 0; i < players.Count; i++)
            {
                string owner = Multiplayer.IsYourPhotonId(players[i].m_PhotonID) ? " (you control)" : " (another player controls)";
                sb.AppendLine($"- {players[i].m_PlayerNameStr}: {players[i].m_PlayerClass.text}{owner} ");
            }
            Context.Send(sb.ToString().TrimEnd(['\r', '\n']));
        }

        public static void ActionStartGame()
        {
            if (!Multiplayer.IsMultiplayer())
            {
                uiFTKButton btn = characterCreateRoot.transform.Find("UIRoot/ButtonRoot/StartButton").GetComponent<uiFTKButton>();
                SelectButton.StartCoroutine(btn, 0.5f);
                return;
            }
            SetAllReady();
        }

        public static void SetAllReady() // auto begins if 3 ready
        {
            bool hasOtherPlayer = false;
            foreach (uiQuickPlayerCreate player in players)
            {
                if (!Multiplayer.IsYourPhotonId(player.m_PhotonID))
                {
                    hasOtherPlayer = true;
                    continue;
                }
                player.SetPlayerReady();
            }
            if (!hasOtherPlayer)
            {
                if (players.Count < 3)
                {
                    Plugin.Logger.LogWarning("beginning game with less than full party");
                    uiStartGame.Instance.EnterFahrul();
                }
            }
        }

        public static void NeuroRandomizeParty()
        {
            if (!Multiplayer.IsMultiplayer()) characterCreateRoot.RandomParty();
            else
            {
                foreach (uiQuickPlayerCreate player in players)
                {
                    if (Multiplayer.IsYourPhotonId(player.m_PhotonID)) player.RandomClass();
                }
                OnPartyRandomized();
            }
        }

        public static void NeuroSetCharacterNames(List<string> names)
        {
            characterCreateRoot.StartCoroutine(ChangeNames(names));
        }

        static IEnumerator ChangeNames(List<string> names)
        {
            int count = 0;
            foreach (uiQuickPlayerCreate player in players)
            {
                if (!Multiplayer.IsYourPhotonId(player.m_PhotonID)) continue;
                string name = names[count];
                count++;
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
            CharacterCustomize.players = [.. players.Where(x => Multiplayer.IsYourPhotonId(x.m_PhotonID))];
            CharacterCustomize.CustomizePlayer(CharacterCustomize.players.First());
            yield break;
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
        }

        static void RegisterWindow(uiCharacterCreateRoot instance)
        {
            if (instance == null)
            {
                Plugin.Logger.LogError("character create root is null");
                return;
            }
            SendPartyDetails();
            if (Multiplayer.IsMultiplayer())
            {
                Plugin.Logger.LogWarning("local multiplayer setup");
                // return;
            }
            instance.StartCoroutine(QuickTimerCallback.WaitRoutine(() => ConfiguePartyAction.RegisterConfigurePartyActions(instance.gameObject, players), instance.gameObject));
        }

// // [Message:Neuro For the King] Player 1, Player 2, Player 3
//         static List<string> GetCharacterNames()
//         {
//             List<string> names = [];
//             foreach (uiQuickPlayerCreate player in characterCreateRoot.m_Players)
//             {
//                 names.Add(player.m_PlayerNameStr);
//             }
//             return names;
//         }

// // [Message:Neuro For the King] Hunter, Minstrel, Hunter
//         static List<string> GetCharacterClasses()
//         {
//             List<string> names = [];
//             foreach (uiQuickPlayerCreate player in characterCreateRoot.m_Players)
//             {
//                 names.Add(player.m_PlayerClass.text);
//             }
//             return names;
//         }
    }
}
