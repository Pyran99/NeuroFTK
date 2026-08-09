using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Pyran.NeuroFTK.Utils;
using UnityEngine;

namespace Pyran.NeuroFTK.NeuroIntegration
{
    public class CameraSpinAction: NeuroAction
    {
        public override string Name => "spin";
        protected override string Description => "SPIN!";
        protected override JsonSchema Schema => null;

        protected override void Execute()
        {
            Camera cam = OverworldCamera.Instance.m_Camera; // overworld
            Context.Send("you are spinning!");
            if (cam.enabled)
            {
                Plugin.Instance.StartCoroutine(CameraUtils.RotateCamera());
                return;
            }
            else
            {
                cam = OverworldCamera.Instance.m_OverlayCamera; // combat
                // if (cam.enabled) Plugin.Instance.StartCoroutine(CameraUtils.CombatRotateCamera());
                if (cam.enabled) Plugin.Instance.StartCoroutine(CameraUtils.CombatRotateCow());
            }
        }

        protected override ExecutionResult Validate(ActionJData actionData)
        {
            return ExecutionResult.Success();
        }
    }

    public class CameraZoomAction(RtsCamera cam) : NeuroAction<float>
    {
        public override string Name => "camera_zoom";
        protected override string Description => "change zoom value of camera (lower value = zoom in)";
        protected override JsonSchema Schema => GetSchema();

        private JsonSchema GetSchema()
        {
            JsonSchema schema = new()
            {
                Type = JsonSchemaType.Object,
                Required = ["zoom"],
                Properties = new()
                {
                    ["zoom"] = new()
                    {
                        Type = JsonSchemaType.Integer,
                        Minimum = cam.MinDistance, // 20
                        Maximum = cam.MaxDistance, // 70
                    }
                }
            };
            return schema;
        }

        protected override void Execute(float parsedData)
        {
            CameraUtils.Zoom(parsedData);
            Context.Send("you have changed the camera zoom to " + parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out float parsedData)
        {
            float data = actionData.Data.Value<float>("zoom");
            parsedData = data;
            return ExecutionResult.Success();
        }
    }
}