using System;
using System.Reflection;
using UnityEngine;

namespace AtlasXR.XR.Passthrough
{
    [DisallowMultipleComponent]
    public sealed class QuestPassthroughController : MonoBehaviour
    {
        [SerializeField] private bool enableOnStart = true;
        [SerializeField] private bool configureCamera = true;
        [SerializeField] private bool enablePassthroughInEditor;
        [SerializeField] private bool preferMetaPassthroughLayer = true;
        [SerializeField] private Color transparentBackground = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color opaqueFallbackBackground = new Color(0.192f, 0.302f, 0.475f, 1f);

        public Camera TargetCamera { get; set; }

        public bool IsPassthroughRequested { get; private set; }

        private Camera configuredCamera;
        private CameraClearFlags previousClearFlags;
        private Color previousBackgroundColor;
        private bool hasPreviousCameraState;

        private void Start()
        {
            if (enableOnStart)
            {
                EnablePassthrough();
            }
        }

        public void EnablePassthrough()
        {
            IsPassthroughRequested = true;

            var camera = TargetCamera != null ? TargetCamera : Camera.main;

            if (!CanAttemptPassthrough())
            {
                ConfigureCameraForOpaqueFallback(camera);
                Debug.Log("AtlasXR passthrough skipped in the Unity Editor. Use a Quest Android build, or enable editor passthrough explicitly if your runtime supports it.");
                return;
            }

            var metaLayerEnabled = preferMetaPassthroughLayer && TryEnableMetaPassthroughLayer(camera);
            var openXrBlendEnabled = TrySetOpenXrEnvironmentBlendMode("AlphaBlend");

            if (metaLayerEnabled)
            {
                ConfigureCameraForPassthrough(camera);
                Debug.Log("AtlasXR passthrough requested through Meta/Oculus passthrough components.");
                return;
            }

            if (openXrBlendEnabled)
            {
                ConfigureCameraForPassthrough(camera);
                Debug.Log("AtlasXR passthrough requested through OpenXR alpha blend mode.");
                return;
            }

            ConfigureCameraForOpaqueFallback(camera);
            Debug.LogWarning("AtlasXR passthrough requested, but no Quest passthrough runtime API was available. Add Meta XR SDK for the native OVRPassthroughLayer path, or verify OpenXR alpha blend support on device.");
        }

        public void DisablePassthrough()
        {
            IsPassthroughRequested = false;
            TrySetOpenXrEnvironmentBlendMode("Opaque");
            TrySetMetaManagerPassthrough(false);
            RestoreCameraState();
        }

        private void ConfigureCameraForPassthrough(Camera camera)
        {
            if (!configureCamera || camera == null)
            {
                return;
            }

            CaptureCameraState(camera);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = transparentBackground;
        }

        private void ConfigureCameraForOpaqueFallback(Camera camera)
        {
            if (!configureCamera || camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = opaqueFallbackBackground;
        }

        private bool CanAttemptPassthrough()
        {
            return !Application.isEditor || enablePassthroughInEditor;
        }

        private void CaptureCameraState(Camera camera)
        {
            if (hasPreviousCameraState && configuredCamera == camera)
            {
                return;
            }

            configuredCamera = camera;
            previousClearFlags = camera.clearFlags;
            previousBackgroundColor = camera.backgroundColor;
            hasPreviousCameraState = true;
        }

        private void RestoreCameraState()
        {
            if (!hasPreviousCameraState || configuredCamera == null)
            {
                return;
            }

            configuredCamera.clearFlags = previousClearFlags;
            configuredCamera.backgroundColor = previousBackgroundColor;
            configuredCamera = null;
            hasPreviousCameraState = false;
        }

        private bool TryEnableMetaPassthroughLayer(Camera camera)
        {
            var managerEnabled = TrySetMetaManagerPassthrough(true);
            var layerEnabled = TryAddMetaPassthroughLayer(camera);
            return managerEnabled || layerEnabled;
        }

        private bool TrySetMetaManagerPassthrough(bool enabled)
        {
            var managerType = FindType("OVRManager");
            if (managerType == null)
            {
                return false;
            }

            var manager = FindFirstObjectByType(managerType) as Component;
            if (manager == null)
            {
                manager = gameObject.AddComponent(managerType);
            }

            var property = managerType.GetProperty("isInsightPassthroughEnabled", BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
            {
                property.SetValue(manager, enabled);
                return true;
            }

            return false;
        }

        private bool TryAddMetaPassthroughLayer(Camera camera)
        {
            var layerType = FindType("OVRPassthroughLayer");
            if (layerType == null)
            {
                return false;
            }

            var host = camera != null ? camera.gameObject : gameObject;
            var layer = host.GetComponent(layerType) as Component;
            if (layer == null)
            {
                layer = host.AddComponent(layerType);
            }

            TrySetEnumProperty(layer, layerType, "overlayType", "Underlay");
            TrySetEnumProperty(layer, layerType, "placement", "Reconstruction");
            return true;
        }

        private static bool TrySetOpenXrEnvironmentBlendMode(string modeName)
        {
            var featureType = FindType("UnityEngine.XR.OpenXR.Features.OpenXRFeature");
            var blendModeType = FindType("UnityEngine.XR.OpenXR.NativeTypes.XrEnvironmentBlendMode");
            if (featureType == null || blendModeType == null)
            {
                return false;
            }

            var method = featureType.GetMethod("SetEnvironmentBlendMode", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null || !Enum.TryParse(blendModeType, modeName, out var blendMode))
            {
                return false;
            }

            try
            {
                method.Invoke(null, new[] { blendMode });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"OpenXR environment blend mode '{modeName}' could not be applied: {exception.Message}");
                return false;
            }
        }

        private static void TrySetEnumProperty(Component component, Type componentType, string propertyName, string valueName)
        {
            var property = componentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
            {
                return;
            }

            if (Enum.TryParse(property.PropertyType, valueName, out var value))
            {
                property.SetValue(component, value);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
