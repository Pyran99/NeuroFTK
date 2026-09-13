using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using HarmonyLib;
using NeuroSdk.Messages.Outgoing;
using Pyran.NeuroFTK.HarmonyPatches;
using UnityEngine;
using UnityEngine.UI;

namespace Pyran.NeuroFTK.GameConfigs
{
    [HarmonyPatch]
    public class CharacterCustomize
    {
        public static List<uiQuickPlayerCreate> players;
        static readonly float cycleDelay = 0.5f;
        static readonly List<uiQuickPlayerCreate> finishedPlayers = [];

        public static void CustomizePlayer(uiQuickPlayerCreate player)
        {
            if (players.Remove(player))
            {
                if (!finishedPlayers.Contains(player)) finishedPlayers.Add(player);
                CustomizeCharacterAction.RegisterWindow(player);
            }
            else Plugin.Logger.LogError($"invalid customize player {player.m_PlayerNameStr}");
        }

        static void ToggleCustomizeMenu(uiQuickPlayerCreate instance, bool enable = false)
        {
            if (enable)
            {
                if (instance.m_Mode != uiQuickPlayerCreate.Mode.Customize)
                {
                    if (instance.m_PlayerInfoRoot == null || instance.m_CustomizeRoot == null || instance.m_CharacterDetails == null) return;
                    instance.SetMode(uiQuickPlayerCreate.Mode.Customize);
                }
            }
            else
            {
                if (instance.m_Mode != uiQuickPlayerCreate.Mode.PlayerInfo)
                {
                    if (instance.m_PlayerInfoRoot == null || instance.m_CustomizeRoot == null || instance.m_CharacterDetails == null) return;
                    instance.SetMode(uiQuickPlayerCreate.Mode.PlayerInfo);
                }
            }
        }

        public static string FixName(string name)
        {
            return name switch
            {
                "helmetMask01" => "helmetBeastman",
                "helmetMask02" => "helmetOwlbear",
                "helmetMask03" => "helmetTriclops",
                "helmetBeastman" => "helmetMask01",
                "helmetOwlbear" => "helmetMask02",
                "helmetTriclops" => "helmetMask03",
                "None" => "default",
                "default" => "None",
                _ => name,
            };
        }

        public static IEnumerator NeuroTryCustomize(uiQuickPlayerCreate instance, object[] args)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            if (Enum.IsDefined(typeof(FTK_playerGameStart.SkinType), args[0]))
            {
                FTK_playerGameStart.SkinType skin = (FTK_playerGameStart.SkinType)Enum.Parse(typeof(FTK_playerGameStart.SkinType), (string)args[0]);
                yield return instance.StartCoroutine(CycleSkinsTo(skin, instance));
            }
            if (Enum.IsDefined(typeof(FTK_customizeArmor.ID), args[1]))
            {
                FTK_customizeArmor.ID armor = (FTK_customizeArmor.ID)Enum.Parse(typeof(FTK_customizeArmor.ID), (string)args[1]);
                yield return instance.StartCoroutine(CycleClothesTo(armor, instance));
            }
            if (Enum.IsDefined(typeof(FTK_customizeHelmet.ID), args[2]))
            {
                FTK_customizeHelmet.ID helmet = (FTK_customizeHelmet.ID)Enum.Parse(typeof(FTK_customizeHelmet.ID), (string)args[2]);
                yield return instance.StartCoroutine(CycleHelmetTo(helmet, instance));
            }
            if (Enum.IsDefined(typeof(FTK_customizeBackpack.ID), args[3]))
            {
                FTK_customizeBackpack.ID backpack = (FTK_customizeBackpack.ID)Enum.Parse(typeof(FTK_customizeBackpack.ID), (string)args[3]);
                yield return instance.StartCoroutine(CycleBackpackTo(backpack, instance));
            }
            yield return instance.StartCoroutine(ChangeArmorColor((string)args[4], instance));
            yield return instance.StartCoroutine(ChangeSkinColor((string)args[5], instance));
            yield return instance.StartCoroutine(ChangeHairColor((string)args[6], instance));
            Context.Send($"finished customize for {instance.m_PlayerNameStr}", true);
            if (players.Count > 0) CustomizePlayer(players.First());
            else
            {
                StringBuilder sb = new();
                foreach (uiQuickPlayerCreate player in finishedPlayers)
                {
                    if (player == null)
                    {
                        Plugin.Logger.LogError($"player was null");
                        continue;
                    }
                    sb.Append($"{player.m_PlayerNameStr} ({player.m_PlayerClass.text}), ");
                    ToggleCustomizeMenu(player, false);
                }
                Context.Send($"finished setting up your party. Tell chat a quick backstory about each of your party members {sb.ToString().TrimEnd([' ', ','])}. (fantasy world setting)");
                finishedPlayers.Clear();
                Plugin.Logger.LogWarning("allowing time for neuro to yap a backstory, auto progressing after time");
                yield return new WaitForSeconds(12f);
                SetupParty.ActionStartGame();
            }
        }

        static IEnumerator CycleSkinsTo(FTK_playerGameStart.SkinType skinType, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            while (instance.m_SkinType != skinType)
            {
                instance.OnSkinClick();
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        static IEnumerator CycleClothesTo(FTK_customizeArmor.ID armorType, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            while (instance.m_CustomOutfit.m_ArmorID != armorType)
            {
                instance.OnArmorClick();
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        static IEnumerator CycleHelmetTo(FTK_customizeHelmet.ID helmetType, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            while (instance.m_CustomOutfit.m_HelmetID != helmetType)
            {
                instance.OnHelmetClick();
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        static IEnumerator CycleBackpackTo(FTK_customizeBackpack.ID backpackType, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            while (instance.m_CustomOutfit.m_BackpackID != backpackType)
            {
                instance.OnBackpackClick();
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        static IEnumerator ChangeArmorColor(string hexColor, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            instance.OnMainColorClick();
            yield return new WaitForSeconds(0.5f);
            uiColorPalette window = instance.m_ImageColorMain.transform.GetComponentInChildren<uiColorPalette>();
            Button[] btns = window.transform.GetComponentsInChildren<Button>(true);
            Image color;
            foreach (Button btn in btns)
            {
                color = btn.GetComponent<Image>();
                if (ColorUtility.ToHtmlStringRGBA(color.color) == hexColor)
                {
                    btn.onClick.Invoke();
                    break;
                }
            }
        }

        static IEnumerator ChangeSkinColor(string hexColor, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            instance.OnSkinColorClick();
            yield return new WaitForSeconds(0.5f);
            uiColorPalette window = instance.m_ImageColorSkin.transform.GetComponentInChildren<uiColorPalette>();
            Button[] btns = window.transform.GetComponentsInChildren<Button>(true);
            Image color;
            foreach (Button btn in btns)
            {
                color = btn.GetComponent<Image>();
                if (ColorUtility.ToHtmlStringRGBA(color.color) == hexColor)
                {
                    btn.onClick.Invoke();
                    break;
                }
            }
        }

        static IEnumerator ChangeHairColor(string hexColor, uiQuickPlayerCreate instance)
        {
            ToggleCustomizeMenu(instance, true);
            yield return null;
            instance.OnHairColorClick();
            yield return new WaitForSeconds(0.5f);
            uiColorPalette window = instance.m_ImageColorHair.transform.GetComponentInChildren<uiColorPalette>();
            Button[] btns = window.transform.GetComponentsInChildren<Button>(true);
            Image color;
            foreach (Button btn in btns)
            {
                color = btn.GetComponent<Image>();
                if (ColorUtility.ToHtmlStringRGBA(color.color) == hexColor)
                {
                    btn.onClick.Invoke();
                    break;
                }
            }
        }



    }
}