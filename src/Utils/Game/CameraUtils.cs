using System.Collections;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class CameraUtils
    {
        public static bool OnCooldown
        {
            get => onCooldown; private set => SetCooldown(value);
        }

        static bool onCooldown = false;
        static bool isRunning = false;
        static Quaternion initialCombatCamera;
        static Quaternion initialCowRotation;
        static Vector3 initialCowPos;
        static readonly float cooldownMs = 10_000f;

        private static bool SetCooldown(bool value)
        {
            onCooldown = value;
            if (value)
            {
                QuickTimerCallback timer = new(Reset, null, cooldownMs);
            }
            return value;
        }

        public static void Reset() => OnCooldown = false;

        public static bool IsOnCooldown(string action = "")
        {
            if (OnCooldown)
            {
                Context.Send($"{action} is on cooldown", true);
            }
            return OnCooldown;
        }

        public static IEnumerator RotateCamera()
        {
            if (isRunning) yield break;
            isRunning = true;
            OnCooldown = true;
            float elapsedTime = 0f;
            Camera cam = OverworldCamera.Instance.m_Camera; // overworld
            if (!cam.enabled) yield break;
            RtsCamera cam3 = cam.GetComponent<RtsCamera>();
            Rigidbody rb = cam3.GetComponent<Rigidbody>();

            float startRot2 = cam3.Rotation;
            if (rb) rb.freezeRotation = false;
            while (elapsedTime < 1.0f)
            {
                float progress = elapsedTime / 1.0f;
                cam3.Rotation = Mathf.Lerp(startRot2, startRot2 + 360, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            cam3.Rotation = startRot2;
            isRunning = false;
            if (rb) rb.freezeRotation = true;
        }

        public static void Zoom(float value)
        {
            Camera cam = FTKHub.Instance.m_OverworldCamera;
            RtsCamera cam3 = cam.GetComponent<RtsCamera>();
            cam3.Distance = Mathf.Clamp(value, cam3.MinDistance, cam3.MaxDistance);
        }


        public static IEnumerator CombatRotateCow()
        {
            if (isRunning) yield break;
            isRunning = true;
            OnCooldown = true;
            CharacterDummy dummy = CharacterData.GetActiveCow().m_CurrentDummy;
            GameObject avatar = dummy.transform.Find("Avatar")?.gameObject;
            if (avatar == null)
            {
                isRunning = false;
                yield break;
            }
            initialCowRotation = avatar.transform.localRotation;
            float startRot2 = initialCowRotation.eulerAngles.y;
            float elapsedTime = 0f;
            while (elapsedTime < 1.0f)
            {
                float progress = elapsedTime / 1.0f;
                avatar.transform.localRotation = Quaternion.Euler(initialCowRotation.eulerAngles.x, startRot2 - 360 * progress, initialCowRotation.eulerAngles.z);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            avatar.transform.localRotation = initialCowRotation;
            isRunning = false;
        }


        public static IEnumerator CombatJumpCow()
        {
            if (isRunning) yield break;
            // if (Battle.StanceBtnInstance == null || !Battle.StanceBtnInstance.m_Initialized) yield break;
            isRunning = true;
            OnCooldown = true;
            CharacterDummy dummy = CharacterData.GetActiveCow().m_CurrentDummy;
            if ((bool)!dummy.m_CharacterOverworld?.m_CharacterStats.m_IsInCombat)
            {
                isRunning = false;
                yield break;
            }
            GameObject avatar = dummy.transform.Find("Avatar")?.gameObject;
            if (avatar == null)
            {
                isRunning = false;
                yield break;
            }
            initialCowPos = avatar.transform.localPosition;
            float elapsedTime = 0f;
            Vector3 target = new(initialCowPos.x, initialCowPos.y + 2, initialCowPos.z);
            while (elapsedTime < 0.5f)
            {
                float progress = elapsedTime / 0.5f;
                avatar.transform.localPosition = Vector3.Lerp(initialCowPos, target, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            avatar.transform.localPosition = target;
            elapsedTime = 0f;
            while (elapsedTime < 0.5f)
            {
                float progress = elapsedTime / 0.5f;
                avatar.transform.localPosition = Vector3.Lerp(target, initialCowPos, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            avatar.transform.localPosition = initialCowPos;
            isRunning = false;
        }

#region CRIME SCENE

        public static IEnumerator CombatRotateCamera()
        {
            if (isRunning) yield break;
            isRunning = true;
            OnCooldown = true;
            OverlayCamera cam = OverworldCamera.Instance.m_OverlayCamera.GetComponent<OverlayCamera>();
            initialCombatCamera = cam.transform.rotation;
            float startRot2 = initialCombatCamera.eulerAngles.z;
            float elapsedTime = 0f;
            while (elapsedTime < 1.0f)
            {
                float progress = elapsedTime / 1.0f;
                cam.transform.rotation = Quaternion.Euler(initialCombatCamera.eulerAngles.x, initialCombatCamera.eulerAngles.y, startRot2 - 360 * progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            cam.transform.rotation = initialCombatCamera;
            isRunning = false;
        }

#endregion

    }
}