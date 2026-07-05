using System.Collections.Generic;
using UnityEngine;

namespace AtlasXR.XR.Hands
{
    [DisallowMultipleComponent]
    public sealed class XRHandVisual : MonoBehaviour
    {
        [SerializeField] private XRHandedness hand = XRHandedness.Right;
        [SerializeField] private Color handColor = new Color(0.68f, 0.86f, 1f, 0.72f);
        [SerializeField] private Color pinchColor = new Color(1f, 0.82f, 0.28f, 0.88f);

        private readonly Transform[] fingers = new Transform[5];
        private readonly Transform[] fingertips = new Transform[5];
        private readonly List<Vector3>[] trackedFingerPositions = new List<Vector3>[XRHandInput.FingerCount];
        private Transform visualRoot;
        private Transform palm;
        private Transform wrist;
        private Material handMaterial;

        public XRHandedness Hand
        {
            get => hand;
            set
            {
                hand = value;
                ApplyHandedness();
            }
        }

        private void Awake()
        {
            for (var index = 0; index < trackedFingerPositions.Length; index++)
            {
                trackedFingerPositions[index] = new List<Vector3>();
            }

            handMaterial = CreateHandMaterial();
            BuildVisual();
            ApplyHandedness();
            UpdateMaterialColor();
        }

        private void OnDestroy()
        {
            if (handMaterial != null)
            {
                Destroy(handMaterial);
            }
        }

        private void Update()
        {
            UpdateTrackedSkeleton();
            UpdateMaterialColor();
        }

        private void BuildVisual()
        {
            visualRoot = new GameObject("Procedural Hand Visual").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = new Vector3(0f, -0.025f, 0.03f);
            visualRoot.localRotation = Quaternion.identity;

            palm = CreatePart("Palm", PrimitiveType.Capsule, visualRoot);
            palm.localPosition = Vector3.zero;
            palm.localRotation = Quaternion.Euler(90f, 0f, 0f);
            palm.localScale = new Vector3(0.075f, 0.055f, 0.032f);

            wrist = CreatePart("Wrist", PrimitiveType.Capsule, visualRoot);
            wrist.localPosition = new Vector3(0f, -0.006f, -0.095f);
            wrist.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wrist.localScale = new Vector3(0.045f, 0.035f, 0.045f);

            CreateFingers();
        }

        private void CreateFingers()
        {
            var xOffsets = new[] { -0.045f, -0.022f, 0f, 0.022f, 0.044f };
            var lengths = new[] { 0.065f, 0.095f, 0.105f, 0.095f, 0.075f };
            var rotations = new[] { -26f, -8f, 0f, 7f, 16f };

            for (var index = 0; index < fingers.Length; index++)
            {
                var finger = CreatePart($"Finger {index + 1}", PrimitiveType.Capsule, visualRoot);
                finger.localPosition = new Vector3(xOffsets[index], 0.002f, 0.07f + lengths[index] * 0.5f);
                finger.localRotation = Quaternion.Euler(90f, rotations[index], 0f);
                finger.localScale = new Vector3(0.011f, lengths[index] * 0.5f, 0.011f);

                var fingertip = CreatePart($"Finger {index + 1} Tip", PrimitiveType.Sphere, finger);
                fingertip.localPosition = new Vector3(0f, 1.02f, 0f);
                fingertip.localRotation = Quaternion.identity;
                fingertip.localScale = new Vector3(1.05f, 0.23f, 1.05f);

                fingers[index] = finger;
                fingertips[index] = fingertip;
            }
        }

        private Transform CreatePart(string partName, PrimitiveType primitiveType, Transform parent)
        {
            var part = GameObject.CreatePrimitive(primitiveType).transform;
            part.name = partName;
            part.SetParent(parent, false);
            part.GetComponent<Renderer>().material = handMaterial;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return part;
        }

        private void ApplyHandedness()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localScale = new Vector3(hand == XRHandedness.Left ? -1f : 1f, 1f, 1f);
        }

        private bool UpdateTrackedSkeleton()
        {
            if (!XRHandInput.TryGetHandSkeleton(hand, out var rootPosition, out var rootRotation, trackedFingerPositions))
            {
                return false;
            }

            visualRoot.localScale = Vector3.one;
            visualRoot.SetPositionAndRotation(
                ConvertTrackingPositionToWorld(rootPosition),
                ConvertTrackingRotationToWorld(rootRotation));

            if (palm != null)
            {
                palm.localPosition = Vector3.zero;
                palm.localRotation = Quaternion.Euler(90f, 0f, 0f);
                palm.localScale = new Vector3(0.075f, 0.055f, 0.032f);
            }

            if (wrist != null)
            {
                wrist.localPosition = new Vector3(0f, 0f, -0.08f);
                wrist.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wrist.localScale = new Vector3(0.045f, 0.032f, 0.04f);
            }

            for (var fingerIndex = 0; fingerIndex < fingers.Length; fingerIndex++)
            {
                UpdateTrackedFinger(fingerIndex);
            }

            return true;
        }

        private void UpdateTrackedFinger(int fingerIndex)
        {
            var finger = fingers[fingerIndex];
            if (finger == null)
            {
                return;
            }

            var positions = trackedFingerPositions[fingerIndex];
            if (positions.Count < 2)
            {
                finger.gameObject.SetActive(false);
                return;
            }

            finger.gameObject.SetActive(true);

            var start = ConvertTrackingPositionToWorld(positions[0]);
            var end = ConvertTrackingPositionToWorld(positions[positions.Count - 1]);
            var direction = end - start;
            var length = direction.magnitude;
            if (length < 0.005f)
            {
                finger.gameObject.SetActive(false);
                return;
            }

            finger.position = start + direction * 0.5f;
            finger.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            finger.localScale = new Vector3(0.011f, length * 0.5f, 0.011f);

            if (fingertips[fingerIndex] != null)
            {
                fingertips[fingerIndex].position = end;
                fingertips[fingerIndex].rotation = finger.rotation;
                fingertips[fingerIndex].localScale = new Vector3(1.05f, 0.23f, 1.05f);
            }
        }

        private Vector3 ConvertTrackingPositionToWorld(Vector3 trackingPosition)
        {
            return transform.parent != null
                ? transform.parent.TransformPoint(trackingPosition)
                : trackingPosition;
        }

        private Quaternion ConvertTrackingRotationToWorld(Quaternion trackingRotation)
        {
            return transform.parent != null
                ? transform.parent.rotation * trackingRotation
                : trackingRotation;
        }

        private void UpdateMaterialColor()
        {
            if (handMaterial == null)
            {
                return;
            }

            var color = IsPinching() ? pinchColor : handColor;
            if (handMaterial.HasProperty("_BaseColor"))
            {
                handMaterial.SetColor("_BaseColor", color);
            }

            if (handMaterial.HasProperty("_Color"))
            {
                handMaterial.SetColor("_Color", color);
            }
        }

        private bool IsPinching()
        {
            return XRHandInput.IsPinching(hand);
        }

        private static Material CreateHandMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.renderQueue = 5000;
            return material;
        }
    }
}
