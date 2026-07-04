using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace AtlasXR.XR.Input
{
    [DisallowMultipleComponent]
    public sealed class XRControllerRay : MonoBehaviour
    {
        private static readonly List<XRNodeState> NodeStates = new List<XRNodeState>();

        [SerializeField] private XRNode controllerNode = XRNode.RightHand;
        [SerializeField] private float maxDistance = 8f;
        [SerializeField] private float rayWidth = 0.025f;
        [SerializeField] private Color idleColor = new Color(0.2f, 0.75f, 1f, 0.85f);
        [SerializeField] private Color hitColor = new Color(1f, 0.78f, 0.15f, 1f);

        private LineRenderer lineRenderer;
        private Transform reticle;
        private Transform originMarker;
        private Material rayMaterial;
        private bool wasTriggerPressed;

        public XRNode ControllerNode
        {
            get => controllerNode;
            set
            {
                if (controllerNode == value)
                {
                    return;
                }

                controllerNode = value;
            }
        }

        private void Awake()
        {
            lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = rayWidth;
            lineRenderer.numCapVertices = 8;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            rayMaterial = CreateRayMaterial();
            lineRenderer.material = rayMaterial;
            lineRenderer.startColor = idleColor;
            lineRenderer.endColor = idleColor;

            originMarker = CreateMarker("Ray Origin", 0.045f);
            reticle = CreateMarker("Ray Reticle", 0.055f);
            ApplyInitialFallbackPosition();
        }

        private void OnEnable()
        {
            Application.onBeforeRender += UpdatePoseAndRay;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= UpdatePoseAndRay;
        }

        private void OnDestroy()
        {
            if (originMarker != null)
            {
                Destroy(originMarker.gameObject);
            }

            if (reticle != null)
            {
                Destroy(reticle.gameObject);
            }

            if (rayMaterial != null)
            {
                Destroy(rayMaterial);
            }
        }

        private void Update()
        {
            UpdatePoseAndRay();
        }

        private void UpdatePoseAndRay()
        {
            if (!UpdatePoseFromInputSystemDevice())
            {
                UpdatePoseFromNodeState();
            }

            UpdateRay();
        }

        private bool UpdatePoseFromInputSystemDevice()
        {
            var controller = FindControllerDevice();
            if (controller == null)
            {
                return false;
            }

            var hasPosition = TryReadVector3(controller, out var position, "pointerPosition", "devicePosition", "position");
            var hasRotation = TryReadQuaternion(controller, out var rotation, "pointerRotation", "deviceRotation", "rotation");

            if (hasPosition)
            {
                transform.localPosition = position;
            }

            if (hasRotation)
            {
                transform.localRotation = rotation;
            }

            return hasPosition || hasRotation;
        }

        private void UpdatePoseFromNodeState()
        {
            InputTracking.GetNodeStates(NodeStates);
            for (var index = 0; index < NodeStates.Count; index++)
            {
                var nodeState = NodeStates[index];
                if (nodeState.nodeType != controllerNode)
                {
                    continue;
                }

                var hasPosition = nodeState.TryGetPosition(out var position);
                var hasRotation = nodeState.TryGetRotation(out var rotation);

                if (hasPosition)
                {
                    transform.localPosition = position;
                }

                if (hasRotation)
                {
                    transform.localRotation = rotation;
                }

                return;
            }
        }

        private void UpdateRay()
        {
            var start = transform.position;
            var end = start + transform.forward * maxDistance;
            var color = idleColor;

            if (Physics.Raycast(start, transform.forward, out var hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
            {
                end = hit.point;
                color = hitColor;

                var triggerPressed = IsTriggerPressed();
                var pressedThisFrame = triggerPressed && !wasTriggerPressed;
                wasTriggerPressed = triggerPressed;

                if (pressedThisFrame && hit.collider.TryGetComponent(out XRRayButton button))
                {
                    button.Press();
                }
            }
            else
            {
                wasTriggerPressed = IsTriggerPressed();
            }

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            SetMaterialColor(color);

            if (originMarker != null)
            {
                originMarker.position = start;
            }

            if (reticle != null)
            {
                reticle.position = end;
                reticle.localScale = Vector3.one * (color == hitColor ? 0.07f : 0.05f);
            }
        }

        private bool IsTriggerPressed()
        {
            var controller = FindControllerDevice();
            if (controller != null &&
                TryReadButton(controller, out var inputSystemPressed, "triggerPressed", "triggerButton"))
            {
                return inputSystemPressed;
            }

            return false;
        }

        private UnityEngine.InputSystem.InputDevice FindControllerDevice()
        {
            var controller = controllerNode == XRNode.LeftHand
                ? XRController.leftHand
                : XRController.rightHand;

            if (controller != null)
            {
                return controller;
            }

            var expectedHand = controllerNode == XRNode.LeftHand ? "LeftHand" : "RightHand";
            var expectedName = controllerNode == XRNode.LeftHand ? "left" : "right";
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (device == null || !(device is XRController))
                {
                    continue;
                }

                if (HasUsage(device, expectedHand) ||
                    ContainsIgnoreCase(device.name, expectedName) ||
                    ContainsIgnoreCase(device.displayName, expectedName) ||
                    ContainsIgnoreCase(device.description.capabilities, expectedHand))
                {
                    return device;
                }
            }

            return null;
        }

        private static bool HasUsage(UnityEngine.InputSystem.InputDevice device, string expectedUsage)
        {
            foreach (var usage in device.usages)
            {
                if (string.Equals(usage.ToString(), expectedUsage, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsIgnoreCase(string value, string expected)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryReadVector3(UnityEngine.InputSystem.InputDevice device, out Vector3 value, params string[] controlNames)
        {
            for (var index = 0; index < controlNames.Length; index++)
            {
                var control = device.TryGetChildControl<Vector3Control>(controlNames[index]);
                if (control == null)
                {
                    continue;
                }

                value = control.ReadValue();
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadQuaternion(UnityEngine.InputSystem.InputDevice device, out Quaternion value, params string[] controlNames)
        {
            for (var index = 0; index < controlNames.Length; index++)
            {
                var control = device.TryGetChildControl<QuaternionControl>(controlNames[index]);
                if (control == null)
                {
                    continue;
                }

                value = control.ReadValue();
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadButton(UnityEngine.InputSystem.InputDevice device, out bool pressed, params string[] controlNames)
        {
            for (var index = 0; index < controlNames.Length; index++)
            {
                var control = device.TryGetChildControl<ButtonControl>(controlNames[index]);
                if (control == null)
                {
                    continue;
                }

                pressed = control.isPressed;
                return true;
            }

            pressed = false;
            return false;
        }

        private void ApplyInitialFallbackPosition()
        {
            var xOffset = controllerNode == XRNode.LeftHand ? -0.24f : 0.24f;
            transform.localPosition = new Vector3(xOffset, 1.25f, 0.35f);
            transform.localRotation = Quaternion.identity;
        }

        private Transform CreateMarker(string markerName, float diameter)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{name} {markerName}";
            marker.transform.localScale = Vector3.one * diameter;
            marker.GetComponent<Renderer>().material = rayMaterial;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return marker.transform;
        }

        private static Material CreateRayMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.renderQueue = 5000;
            return material;
        }

        private void SetMaterialColor(Color color)
        {
            if (rayMaterial == null)
            {
                return;
            }

            if (rayMaterial.HasProperty("_BaseColor"))
            {
                rayMaterial.SetColor("_BaseColor", color);
            }

            if (rayMaterial.HasProperty("_Color"))
            {
                rayMaterial.SetColor("_Color", color);
            }
        }

    }
}
