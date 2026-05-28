using FTKHelp;
using HarmonyLib;
using UnityEngine;

namespace NeuroFTK.HarmonyPatches.AutomatedActions;

[HarmonyPatch]
public class SetSettingsOptions
{
    public static void InitializeCustomSettings()
    {
        PlayerPrefs.SetInt("AutoEndTurn", 1);
        PlayerPrefs.SetInt(FTKTutorial.PREF_PROMPT_SHOW_TUTORIAL, 0);
        PlayerPrefs.SetInt("LockCursor", 0);
        PlayerPrefs.SetInt("TurboMode", 1);
        Plugin.Logger.LogMessage("Game setting prefs set");
    }

    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.LoadPreferences))]
    [HarmonyPrefix]
    static void SetInitialAudio(AudioManager __instance)
    {
        // if no player prefs this will stop default volume being 100
        __instance.m_DefaultMusicVolume = 35f;
        __instance.m_DefaultFXVolume = 35f;
    }

}