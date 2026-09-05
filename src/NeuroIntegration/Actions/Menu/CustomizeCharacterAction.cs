using System;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using NeuroSdk;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;
using WebSocketSharp;

namespace Pyran.NeuroFTK.GameConfigs
{
    public class CustomizeCharacterAction(uiQuickPlayerCreate _instance, string characterName) : NeuroAction<object[]>
    {
        public static ActionWindow RegisterWindow(uiQuickPlayerCreate instance)
        {
            ActionWindow window = ActionWindow.Create(instance.transform.parent.gameObject);
            window.AddAction(new CustomizeCharacterAction(instance, instance.m_PlayerNameStr));
            window.SetForce(0, "customize this characters model, clothes, and colors. Colors are shown as HTML codes. you should choose different clothes_color for each character to help chat distinguish them", "", true);
            window.Register();
            return window;
        }

        public override string Name => $"customize_{characterName.Replace(" ", "_").ToLower()}";
        protected override string Description => "customize the visual aspects of the character";
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
                    ["helmet"] = QJS.Enum(GetHelmets().Select(x => CharacterCustomize.FixName(x.ToString()))), // "helmetMask01" => "helmetBeastman"
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
            if (actionData.Data == null) return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedMissingRequiredParameter.Format("data"));
            Plugin.Logger.LogWarning($"data: {actionData.Data}");
            parsedData[0] = skinTypes.Contains(actionData.Data.Value<string>("model")) ? actionData.Data.Value<string>("model") : FTK_playerGameStart.SkinType.Male.ToString();
            parsedData[1] = CharacterCustomize.FixName( armorTypes.Contains(actionData.Data.Value<string>("clothes")) ? actionData.Data.Value<string>("clothes") : "default" );
            parsedData[2] = CharacterCustomize.FixName( helmetTypes.Contains(actionData.Data.Value<string>("helmet")) ? actionData.Data.Value<string>("helmet") : "default" );  // "helmetBeastman" => "helmetMask01" for parsing
            parsedData[3] = CharacterCustomize.FixName( backpackTypes.Contains(actionData.Data.Value<string>("backpack")) ? actionData.Data.Value<string>("backpack") : "default" );
            parsedData[4] = mainColors.Contains(actionData.Data.Value<string>("clothes_color")) ? actionData.Data.Value<string>("clothes_color") : ColorUtility.ToHtmlStringRGBA(_instance.m_MainColorArray.First());
            parsedData[5] = skinTones.Contains(actionData.Data.Value<string>("skin_tone")) ? actionData.Data.Value<string>("skin_tone") : ColorUtility.ToHtmlStringRGBA(_instance.m_SkinColorArray.First());
            parsedData[6] = hairColors.Contains(actionData.Data.Value<string>("hair_color")) ? actionData.Data.Value<string>("hair_color") : ColorUtility.ToHtmlStringRGBA(_instance.m_HairColorArray.First());
            return ExecutionResult.Success();
        }

        protected override void Execute(object[] parsedData)
        {
            string print = string.Join(", ", [.. parsedData.Select(x => x.ToString())]);
            Plugin.Logger.LogWarning($"customize result = {print}");
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