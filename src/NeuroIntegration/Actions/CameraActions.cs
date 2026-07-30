using System;
using NeuroSdk.Actions;
using NeuroSdk.Json;
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
            Camera cam = FTKHub.Instance.m_OverworldCamera;
            if (cam.enabled)
            {
                Plugin.Instance.StartCoroutine(CameraUtils.RotateCamera());
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
                        Type = JsonSchemaType.Float,
                        Minimum = cam.MinDistance,
                        Maximum = cam.MaxDistance,
                    }
                }
            };
            return schema;
        }

        protected override void Execute(float parsedData)
        {
            CameraUtils.Zoom(parsedData);
        }

        protected override ExecutionResult Validate(ActionJData actionData, out float parsedData)
        {
            float data = actionData.Data.Value<float>("zoom");
            parsedData = data;
            return ExecutionResult.Success();
        }
    }
}