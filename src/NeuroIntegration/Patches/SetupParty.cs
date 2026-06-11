using System.Collections;
using System.Collections.Generic;
using Google2u;
using GridEditor;
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

        [HarmonyPatch(typeof(FTKHub), nameof(FTKHub.EnterFahrul))]
        [HarmonyPostfix]
        static void EnteringWorld()
        {
            Context.Send("entering the world of Fahrul", true);
        }

        static void OnPartyVisible()
        {
            SendPartyDetails();
            ConfigueParty.RegisterConfigurePartyActions(characterCreateRoot.gameObject);
        }

        static void SendPartyDetails()
        {
            FTK_playerGameStartDB db = FTK_playerGameStartDB.GetDB();
            string data = "";
            foreach (uiQuickPlayerCreate player in players)
            {
                List<string> details = GetClassDetails((FTK_playerGameStart.ID)player.m_ClassID, db);
                string joined = string.Join(", ", [.. details]);
                joined = StringReplace.ReplaceNewLine(joined);
                data += joined;
            }
            Context.Send($"details about the classes: {data}");
            // { "Player 1: class: Hunter },
            List<string> names = GetCharacterNames();
            List<string> classes = GetCharacterClasses();
            data = "your party setup is: ";
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

        static List<string> GetClassDetails(FTK_playerGameStart.ID id, FTK_playerGameStartDB db)
        {
            FTK_playerGameStart entry = db.GetEntry(id);
            List<string> msg = [];
            string name = entry.GetDisplayName();
            float statBonus = GameFlow.Instance.GameDif.m_StatBonus;
            string toughness = FTKUtil.RoundToInt((entry._toughness + statBonus) * 100f).ToString();
            string fortitude = FTKUtil.RoundToInt((entry._fortitude + statBonus) * 100f).ToString();
            string talent = FTKUtil.RoundToInt((entry._talent + statBonus) * 100f).ToString();
            string awareness = FTKUtil.RoundToInt((entry._awareness + statBonus) * 100f).ToString();
            string quickness = FTKUtil.RoundToInt((entry._quickness + statBonus) * 100f).ToString();
            string vitality = FTKUtil.RoundToInt((entry._vitality + statBonus) * 100f).ToString();
            string classFlavor = FTKHub.Localized<TextCharacters>(entry.m_Flavor);
            string classAbility = entry.m_CharacterSkills.GetSkillDisplay(false);
            msg.Add($"{{name: {name}");
            msg.Add($"class description: {classFlavor}");
            msg.Add($"gold: {entry._startinggold + GameFlow.Instance.GameDif.m_ExtraGold}");
            if (entry.m_StartWeapon != FTK_itembase.ID.None)
            {
                msg.Add($"starting weapon: {FTKHub.Instance.GetItemDisplayName(entry.m_StartWeapon)}");
            }
            string items = "";
            foreach (FTK_itembase.ID _id in entry.m_StartItems)
            {
                items += $"{FTKHub.Instance.GetItemDisplayName(_id)}, ";
            }
            msg.Add($"starting items: {{{items}}}");
            msg.Add($"toughness: {toughness}");
            msg.Add($"fortitude: {fortitude}");
            msg.Add($"talent: {talent}");
            msg.Add($"awareness: {awareness}");
            msg.Add($"quickness: {quickness}");
            msg.Add($"vitality: {vitality}");
            msg.Add($"class abilities: {classAbility}}}. ");
            return msg;
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
            uiFTKButton btn = characterCreateRoot.transform.Find("UIRoot/ButtonRoot/StartButton").GetComponent<uiFTKButton>();
            SelectButton.StartCoroutine(characterCreateRoot, btn, 0.5f);
            // uiStartGame.Instance.EnterGame(); // maybe EnterFahrul()?
            //  KeyNotFoundException: The given key was not present in the dictionary.
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
            yield return new WaitForSeconds(0.5f);
            SendPartyDetails();
            Context.Send("tell chat about your party members while the game begins");
            yield return new WaitForSeconds(8f);
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

