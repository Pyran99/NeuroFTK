using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.NeuroIntegration;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    [HarmonyPatch]
    public class ChooseRewardMenu
    {
        static ActionWindow window = null;
        static List<uiChooseRewardButton> buttons = [];
        static string title = "";
        static bool isOwner = false;
        public static string teamState = "";
        static readonly bool onlyOccupiedRealms = true;

        [HarmonyPatch(typeof(uiChooseRewardMenu), "BaseInitialize")]
        [HarmonyPostfix]
        static void Init(string _title, uiChooseRewardMenu.RewardType _rType, CharacterOverworld _cow)
        {
            buttons.Clear();
            title = _title;
            isOwner = Multiplayer.IsYourCow(_cow);
        }

        [HarmonyPatch(typeof(uiChooseRewardMenu), "BaseInitialize2")]
        [HarmonyPostfix]
        static void Init2(uiChooseRewardMenu __instance, List<uiChooseRewardButton> ___m_AllButtons)
        {
            if (!isOwner) return;
            if (title.IsNullOrEmpty())
            {
                Plugin.Logger.LogError("reward title was empty");
                return;
            }
            buttons = [.. ___m_AllButtons];
            Plugin.Logger.LogMessage($"{string.Join(", ", [.. buttons.Select(x => x.m_Text.text)])}");
            Dictionary<string, uiChooseRewardButton> dict = buttons.ToDictionary(x => x.m_Text.text);
            // if (buttons.Count == 1) // only cancel
            // {
            //     uiChooseRewardButton first = buttons.First();
            //     Plugin.Logger.LogWarning("only 1 reward button " + first.m_Text.text);
            //     Context.Send($"only 1 option in this menu, selecting {first.m_Text.text}", true);
            //     SelectButton.StartCoroutine(first, 1.0f);
            //     return;
            // }
            if (buttons.Count > 1 && dict.ContainsKey("Cancel")) dict.Remove("Cancel"); // assume always choose valid
            if (dict.Count == 0)
            {
                Plugin.Logger.LogError("no valid reward buttons");
                Context.Send("there were no options from the reward menu", true);
                __instance.Close();
                return;
            }
            QuickTimerCallback timer = new(() => CreateAction(__instance, dict, title), __instance.m_DisplayRoot.gameObject);
        }

        [HarmonyPatch(typeof(uiChooseRewardMenu), nameof(uiChooseRewardMenu.Close))]
        [HarmonyPrefix]
        static void MenuClosed() // alt to unregister
        {
            buttons.Clear();
            Object.Destroy(window);
        }

        [HarmonyPatch(typeof(MiniEncounterMenuBase), nameof(MiniEncounterMenuBase.CultDeviceCallBack))]
        [HarmonyPostfix]
        static void CultDeviceRoll(uiSlotLegend.SlotOutput _output)
        {
            if (_output.m_Passed)
            {
                Context.Send(StringMessages.CultDeviceDestroyed);
                return;
            }
            Context.Send(StringMessages.CultDeviceDestroyedFail);
        }

        [HarmonyPatch(typeof(MiniEncounterMenuBase), nameof(MiniEncounterMenuBase.FairyFountainCallBack))]
        [HarmonyPostfix]
        static void FairyFountainRoll(uiSlotLegend.SlotOutput _output)
        {
            Plugin.Logger.LogWarning("fairy fountain callback NYI");
            if (_output.m_Meaning == FTK_slotOutputMeaning.ID.Fight)
            {
                // Context.Send();
                return;
            }
            // Context.Send();
        }

        static void CreateAction(uiChooseRewardMenu _instance, Dictionary<string, uiChooseRewardButton> _buttons, string _title)
        {
            foreach (uiChooseRewardButton btn in buttons)
            {
                if (!btn.isActiveAndEnabled) return;
            }
            Dictionary<string, uiChooseRewardButton> validChoices = new(_buttons);
            if (_title == "Respawn In")
            {
                string ctx = GetRespawnData(validChoices);
                Context.Send(ctx);
            }
            window = RewardMenuAction.RegisterActions(_instance, validChoices, _title);
            UnregisterDisabledObject.QuickCreate(_instance.gameObject, window, false);
        }

        static string GetRespawnData(Dictionary<string, uiChooseRewardButton> validChoices)
        {
            StringBuilder sb = new();
            HexLand ownHex = GameLogic.Instance.GetCurrentCOW().GetHexLand();
            // FTK_realm.ID currentRealm = ownHex.GetRealm();
            List<FTK_realm.ID> cowRealms = [];
            List<MiniHexInfo> towns = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Town);
            foreach (CharacterOverworld cow in FTKHub.Instance.m_CharacterOverworlds)
            {
                HexLand hex = cow.GetHexLand();
                if (!cowRealms.Contains(hex.GetRealm()))
                {
                    cowRealms.Add(hex.GetRealm());
                }
                float dist = float.PositiveInfinity;
                MiniHexTown closest = null;
                foreach (MiniHexTown town in towns.Cast<MiniHexTown>())
                {
                    float dist2 = HexLand.Distance(hex, town.m_HexLand);
                    if (dist2 < dist)
                    {
                        dist = dist2;
                        closest = town;
                    }
                }
                if (ownHex == hex)
                {
                    sb.Append($" the reviving character {CharacterData.GetCharacterName(cow)} is near {closest.GetPOIDisplayValue()}.");
                    continue;
                }
                sb.Append($" {CharacterData.GetCharacterName(cow)} is near {closest.GetPOIDisplayValue()}.");
            }
            for (int i = 0; i < towns.Count; i++)
            {
                MiniHexTown town = (MiniHexTown)towns[i];
                if (town.m_VisitedBy.Count > 0)
                {
                    if (onlyOccupiedRealms && !cowRealms.Contains(town.m_HexLand.GetRealm()))
                    {
                        validChoices.Remove(town.GetPOIDisplayValue());
                    }
                }
            }
            if (validChoices.Count == 0) Plugin.Logger.LogError("there were no valid towns to revive in?");
            return sb.ToString();
        }

        [HarmonyPatch(typeof(GameFlow), nameof(GameFlow.SetLifePool))]
        [HarmonyPostfix]
        static void LifePoolChanged(int _set)
        {
            Context.Send($"life pool is now {Mathf.Clamp(_set, 0, GameFlow.Instance.MaxLifePool)}");
        }

        [HarmonyPatch(typeof(GameFlow), nameof(GameFlow.RemoveScourge))]
        [HarmonyPostfix]
        static void RemoveScourge()
        {
            Context.Send($"removed 1 scourge event");
        }

        [HarmonyPatch(typeof(uiChooseRewardMenu), nameof(uiChooseRewardMenu.AcceptChooseReward))]
        [HarmonyPostfix]
        static void Reward(object _v)
        {
            object[] array = (object[])_v;
            ChooseType type = (ChooseType)array[0];
            switch (type)
            {
                case ChooseType.Life:
                    break;
                case ChooseType.Chaos:
                case ChooseType.Alignment:
                    // Context.Send($"reduced chaos level"); // generated a portrait msg instead
                    break;
                case ChooseType.Scourge:
                    break;
                case ChooseType.EquipItemCombat:
                    break;
                case ChooseType.LaunchBoatItem:
                    break;
                case ChooseType.ModArmor:
                    break;
                case ChooseType.ModResist:
                    break;
                case ChooseType.ModAwareness:
                    break;
                case ChooseType.ModEvade:
                    break;
                case ChooseType.ModFortitude:
                    break;
                case ChooseType.ModLuck:
                    break;
                case ChooseType.ModMaxFocus:
                    break;
                case ChooseType.ModMaxHealth:
                    break;
                case ChooseType.ModQuickness:
                    break;
                case ChooseType.ModTalent:
                    break;
                case ChooseType.ModToughness:
                    break;
                case ChooseType.ModVitality:
                    break;
                case ChooseType.Gold:
                    break;
                case ChooseType.XP:
                    break;
            }
        }
    }
}