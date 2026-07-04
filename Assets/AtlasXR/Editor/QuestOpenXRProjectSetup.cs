using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AtlasXR.Editor
{
    public static class QuestOpenXRProjectSetup
    {
        [MenuItem("AtlasXR/Setup/Configure Quest 2 OpenXR")]
        public static void ConfigureQuestOpenXR()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.atlasxr.maintenance");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            TryEnableOpenXRLoader();

            AssetDatabase.SaveAssets();
            Debug.Log("AtlasXR Quest 2 OpenXR setup applied. Verify Project Settings > XR Plug-in Management > Android has OpenXR enabled.");
        }

        private static void TryEnableOpenXRLoader()
        {
            var xrPackageMetadataStore = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (xrPackageMetadataStore == null)
            {
                Debug.LogWarning("XR Management editor metadata APIs were not found. Enable OpenXR manually in XR Plug-in Management.");
                return;
            }

            var assignLoader = xrPackageMetadataStore.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "AssignLoader" &&
                    method.GetParameters().Length == 3);

            if (assignLoader == null)
            {
                Debug.LogWarning("XR Management AssignLoader API was not found. Enable OpenXR manually in XR Plug-in Management.");
                return;
            }

            var openXRLoaderType = FindType("UnityEngine.XR.OpenXR.OpenXRLoader");
            if (openXRLoaderType == null)
            {
                Debug.LogWarning("OpenXRLoader type was not found. Confirm com.unity.xr.openxr is installed.");
                return;
            }

            try
            {
                var result = assignLoader.Invoke(null, new object[]
                {
                    openXRLoaderType.FullName,
                    BuildTargetGroup.Android,
                    0
                });

                if (result is bool assigned && !assigned)
                {
                    Debug.LogWarning("OpenXR loader was not assigned automatically. Enable it manually in XR Plug-in Management for Android.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"OpenXR loader assignment failed: {exception.Message}");
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
