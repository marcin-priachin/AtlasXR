using AtlasXR.XR.Input;
using AtlasXR.XR.Hands;
using AtlasXR.XR.Passthrough;
using UnityEngine;
using UnityEngine.XR;

namespace AtlasXR.App.Bootstrap
{
    public enum XRInteractionMode
    {
        Controllers,
        Hands,
        ControllersAndHands
    }

    public sealed class QuestOpenXRRuntimeBootstrapper : MonoBehaviour
    {
        [SerializeField] private XRInteractionMode interactionMode = XRInteractionMode.Controllers;
        [SerializeField] private bool createHeadsetPanel = true;
        [SerializeField] private bool enableQuestPassthrough = true;
        [SerializeField] private Vector3 rigPosition = new Vector3(0f, 0f, -1.6f);

        private Transform xrOrigin;

        private void Start()
        {
            BuildRuntimeRig();
        }

        public void Configure(XRInteractionMode selectedInteractionMode)
        {
            interactionMode = selectedInteractionMode;
        }

        private void BuildRuntimeRig()
        {
            var cameraTransform = EnsureTrackedCamera();
            if (interactionMode == XRInteractionMode.Controllers ||
                interactionMode == XRInteractionMode.ControllersAndHands)
            {
                EnsureControllerRay("Left XR Controller Ray", XRNode.LeftHand);
                EnsureControllerRay("Right XR Controller Ray", XRNode.RightHand);
            }

            if (interactionMode == XRInteractionMode.Hands ||
                interactionMode == XRInteractionMode.ControllersAndHands)
            {
                EnsureHandRay("Left XR Hand Ray", XRHandedness.Left);
                EnsureHandRay("Right XR Hand Ray", XRHandedness.Right);
            }

            if (createHeadsetPanel && FindFirstObjectByType<XRProcedurePanel>() == null)
            {
                var panel = new GameObject("XR Procedure Panel");
                panel.AddComponent<XRProcedurePanel>().FollowTarget = cameraTransform;
            }

            if (enableQuestPassthrough)
            {
                EnsureQuestPassthrough(cameraTransform.GetComponent<Camera>());
            }
        }

        private Transform EnsureTrackedCamera()
        {
            xrOrigin = EnsureXROrigin();
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.transform.SetParent(xrOrigin, false);
            mainCamera.transform.localPosition = Vector3.up * 1.6f;
            mainCamera.transform.localRotation = Quaternion.identity;

            if (mainCamera.GetComponent<XRHeadPoseDriver>() == null)
            {
                mainCamera.gameObject.AddComponent<XRHeadPoseDriver>();
            }
            return mainCamera.transform;
        }

        private void EnsureQuestPassthrough(Camera targetCamera)
        {
            var passthrough = GetComponent<QuestPassthroughController>();
            if (passthrough == null)
            {
                passthrough = gameObject.AddComponent<QuestPassthroughController>();
            }

            passthrough.TargetCamera = targetCamera;
        }

        private void EnsureControllerRay(string name, XRNode node)
        {
            if (GameObject.Find(name) != null)
            {
                return;
            }

            var rayObject = new GameObject(name);
            rayObject.transform.SetParent(xrOrigin, false);
            var ray = rayObject.AddComponent<XRControllerRay>();
            ray.ControllerNode = node;
        }

        private void EnsureHandRay(string name, XRHandedness hand)
        {
            if (GameObject.Find(name) != null)
            {
                return;
            }

            var rayObject = new GameObject(name);
            rayObject.transform.SetParent(xrOrigin, false);
            var ray = rayObject.AddComponent<XRHandRay>();
            ray.Hand = hand;

            var visual = rayObject.AddComponent<XRHandVisual>();
            visual.Hand = hand;
        }

        private Transform EnsureXROrigin()
        {
            var origin = GameObject.Find("AtlasXR OpenXR Origin");
            if (origin == null)
            {
                origin = new GameObject("AtlasXR OpenXR Origin");
            }

            origin.transform.position = rigPosition;
            origin.transform.rotation = Quaternion.identity;
            return origin.transform;
        }
    }
}
