using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.GameConfigs
{
    public class ChangeClass(List<uiQuickPlayerCreate> _instances) : NeuroAction<object[]>
    {
        public override string Name => "change_class";
        protected override string Description => "change any of your characters class. (Use this if your partner wants you to change your class)";
        protected override JsonSchema Schema => GetSchema();

        List<string> classes = [];

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["character", "class"],
                Properties = new()
                {
                    ["character"] = QJS.Enum(_instances.Select(x => x.m_PlayerNameInput.text)),
                    ["class"] = QJS.Enum(GetValidClasses()),
                }
            };
            return schema;
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[2];
            if (actionData.Data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedNoData);
            string character = actionData.Data.Value<string>("character");
            if (!_instances.Any(x => x.m_PlayerNameInput.text == character))
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("character"));
            }
            string _class = actionData.Data.Value<string>("class");
            if (!classes.Contains(_class))
            {
                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidParameter.Format("class"));
            }
            parsedData[0] = character;
            parsedData[1] = _class;
            return ExecutionResult.Success();
        }

        protected override void Execute(object[] parsedData)
        {
            foreach (uiQuickPlayerCreate player in _instances)
            {
                if (player.m_PlayerNameInput.text != (string)parsedData[0]) continue;
                player.StartCoroutine(CycleToClass(player, (string)parsedData[1]));
                break;
            }
        }

        IEnumerable<string> GetValidClasses()
        {
            classes = [];
            FTK_playerGameStartDB db = FTK_playerGameStartDB.GetDB();
            foreach (FTK_playerGameStart.ID type in Enum.GetValues(typeof(FTK_playerGameStart.ID)))
            {
                if (type == FTK_playerGameStart.ID.None || type == FTK_playerGameStart.ID.professor) continue;
                if (db.IsUnlock(type) && db.IsReveal(type)) classes.Add(type.ToString());
            }
            Plugin.Logger.LogMessage(string.Join(", ", [.. classes.Select(x => x)]));
            return classes;
        }

        IEnumerator CycleToClass(uiQuickPlayerCreate player, string _class)
        {
            player.RPCAllSelf("SetReady", [false]);
            FTK_playerGameStart.ID target = (FTK_playerGameStart.ID)Enum.Parse(typeof(FTK_playerGameStart.ID), _class, true);
            Plugin.Logger.LogMessage($"Current class: {player.m_PlayerClass.text}, Target class: {target}");
            int count = 0;
            while (player.m_ClassID != (int)target)
            {
                player.OnClassClick();
                count++;
                if (count > 20)
                {
                    Plugin.Logger.LogError($"Failed to find class: {_class}");
                    Context.Send($"Failed to find class {_class}, resetting to blacksmith", true);
                    target = FTK_playerGameStart.ID.blacksmith;
                    count = 0;
                }
                yield return new WaitForSeconds(0.5f);
            }
            player.RPCAllSelf("SetReady", [true]);
            Context.Send($"Set {player.m_PlayerNameStr} to {_class}", true);
        }
    }

    public class CustomizeCharacterAction(uiQuickPlayerCreate _instance, string characterName) : NeuroAction<object[]>
    {
        public static ActionWindow RegisterWindow(uiQuickPlayerCreate instance)
        {
            ActionWindow window = ActionWindow.Create(instance.transform.parent.gameObject);
            window.AddAction(new CustomizeCharacterAction(instance, instance.m_PlayerNameStr));
            window.SetForce(0, "customize this characters model, clothes, and colors. you should choose different clothes_color for each character to help chat distinguish them", "", true);
            window.Register();
            UnregisterDisabledObject.QuickCreate(instance.gameObject, window);
            return window;
        }

        public override string Name => $"customize_{characterName.Replace(" ", "_").ToLower()}";
        protected override string Description => $"customize the visual aspects of {characterName}. Colors are listed as HTML codes";
        protected override JsonSchema Schema => GetSchema();

        List<string> skinTypes = [];
        List<string> armorTypes = [];
        List<string> helmetTypes = [];
        List<string> backpackTypes = [];
        string[] mainColors = [];
        string[] skinTones = [];
        string[] hairColors = [];

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["model", "clothes", "helmet", "backpack", "clothes_color", "skin_tone", "hair_color"],
                Properties = new()
                {
                    ["model"] = QJS.Enum(GetSkins().Select(x => x.ToString())),
                    ["clothes"] = QJS.Enum(GetArmors().Select(x => CharacterCustomize.FixName(x.ToString()))),
                    ["helmet"] = QJS.Enum(GetHelmets().Select(x => CharacterCustomize.FixName(x.ToString()))),
                    ["backpack"] = QJS.Enum(GetBackpacks().Select(x => CharacterCustomize.FixName(x.ToString()))),
                    ["clothes_color"] = QJS.Enum(GetMainColors(_instance).Select(x => x)),
                    ["skin_tone"] = QJS.Enum(GetSkinTones(_instance).Select(x => x)),
                    ["hair_color"] = QJS.Enum(GetHairColors(_instance).Select(x => x))
                }
            };
            return schema;
        }

        protected override ExecutionResult Validate(ActionJData actionData, out object[] parsedData)
        {
            parsedData = new object[7];
            if (actionData.Data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedNoData);
            Plugin.Logger.LogWarning($"data: {actionData.Data}");
            parsedData[0] = skinTypes.Contains(actionData.Data.Value<string>("model")) ? actionData.Data.Value<string>("model") : FTK_playerGameStart.SkinType.Male.ToString();
            string clothes = CharacterCustomize.FixName(actionData.Data.Value<string>("clothes") ?? "default");
            string helmet = CharacterCustomize.FixName(actionData.Data.Value<string>("helmet") ?? "default"); // "helmetBeastman" => "helmetMask01" for parsing
            string backpack = CharacterCustomize.FixName(actionData.Data.Value<string>("backpack") ?? "default");
            parsedData[1] = armorTypes.Contains(clothes) ? clothes : "default";
            parsedData[2] = helmetTypes.Contains(helmet) ? helmet : "default";
            parsedData[3] = backpackTypes.Contains(backpack) ? backpack : "default";
            parsedData[4] = mainColors.Contains(actionData.Data.Value<string>("clothes_color")) ? actionData.Data.Value<string>("clothes_color") : ColorUtility.ToHtmlStringRGBA(_instance.m_MainColorArray.First());
            parsedData[5] = skinTones.Contains(actionData.Data.Value<string>("skin_tone")) ? actionData.Data.Value<string>("skin_tone") : ColorUtility.ToHtmlStringRGBA(_instance.m_SkinColorArray.First());
            parsedData[6] = hairColors.Contains(actionData.Data.Value<string>("hair_color")) ? actionData.Data.Value<string>("hair_color") : ColorUtility.ToHtmlStringRGBA(_instance.m_HairColorArray.First());
            return ExecutionResult.Success();
        }

        protected override void Execute(object[] parsedData)
        {
            string print = string.Join(", ", [.. parsedData.Select(x => x.ToString())]);
            Context.Send($"customizing {_instance.m_PlayerNameStr} with values: {print}", true);
            _instance.StartCoroutine(CharacterCustomize.NeuroTryCustomize(_instance, parsedData));
        }

        IEnumerable<string> GetSkins()
        {
            skinTypes = [];
            FTK_loreExtraUnlockDB db = FTK_loreExtraUnlockDB.GetDB();
            foreach (FTK_playerGameStart.SkinType type in Enum.GetValues(typeof(FTK_playerGameStart.SkinType)))
            {
                if (db.IsSkinUnlocked(type)) skinTypes.Add(type.ToString());
            }
            return skinTypes;
        }

        IEnumerable<string> GetArmors()
        {
            armorTypes = [];
            FTK_loreExtraUnlockDB db = FTK_loreExtraUnlockDB.GetDB();
            armorTypes.Add(FTK_customizeArmor.ID.None.ToString());
            foreach (FTK_customizeArmor.ID type in Enum.GetValues(typeof(FTK_customizeArmor.ID)))
            {
                if (db.IsArmorUnlocked(type)) armorTypes.Add(type.ToString());
            }
            return armorTypes;
        }

        IEnumerable<string> GetHelmets()
        {
            helmetTypes = [];
            FTK_loreExtraUnlockDB db = FTK_loreExtraUnlockDB.GetDB();
            helmetTypes.Add(FTK_customizeHelmet.ID.None.ToString());
            foreach (FTK_customizeHelmet.ID type in Enum.GetValues(typeof(FTK_customizeHelmet.ID)))
            {
                if (db.IsHelmetUnlocked(type)) helmetTypes.Add(type.ToString());
            }
            return helmetTypes;
        }

        IEnumerable<string> GetBackpacks()
        {
            backpackTypes = [];
            FTK_loreExtraUnlockDB db = FTK_loreExtraUnlockDB.GetDB();
            backpackTypes.Add(FTK_customizeBackpack.ID.None.ToString());
            foreach (FTK_customizeBackpack.ID type in Enum.GetValues(typeof(FTK_customizeBackpack.ID)))
            {
                if (db.IsBackpackUnlocked(type)) backpackTypes.Add(type.ToString());
            }
            return backpackTypes;
        }

        string[] GetMainColors(uiQuickPlayerCreate instance)
        {
            mainColors = [.. instance.m_MainColorArray.Select(ColorUtility.ToHtmlStringRGBA)];
            return mainColors;
        }

        string[] GetSkinTones(uiQuickPlayerCreate instance)
        {
            skinTones = [.. instance.m_SkinColorArray.Select(ColorUtility.ToHtmlStringRGBA)];
            return skinTones;
        }

        string[] GetHairColors(uiQuickPlayerCreate instance)
        {
            hairColors = [.. instance.m_HairColorArray.Select(ColorUtility.ToHtmlStringRGBA)];
            return hairColors;
        }
    }
}