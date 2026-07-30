using System.Collections;
using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class CameraUtils
    {
        static bool isRunning = false;

        public static IEnumerator RotateCamera()
        {
            if (isRunning) yield break;
            isRunning = true;
            float elapsedTime = 0f;
            Camera cam = FTKHub.Instance.m_OverworldCamera;
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
        
    }
}