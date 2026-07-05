using AtlasXR.XR.Input;
using UnityEngine;

namespace AtlasXR.XR.Hands
{
    public enum XRHandedness
    {
        Left,
        Right
    }

    [DisallowMultipleComponent]
    public sealed class XRHandRay : MonoBehaviour
    {
        [SerializeField] private XRHandedness hand = XRHandedness.Right;
        [SerializeField] private float maxDistance = 8f;
        [SerializeField] private float rayWidth = 0.018f;
        [SerializeField] private Color idleColor = new Color(0.35f, 1f, 0.62f, 0.85f);
        [SerializeField] private Color hitColor = new Color(1f, 0.78f, 0.15f, 1f);

        private LineRenderer lineRenderer;
        private Transform reticle;
        private Transform originMarker;
        private Material rayMaterial;
        private bool wasSelectPressed;

        public XRHandedness Hand
        {
            get => hand;
            set => hand = value;
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

            originMarker = CreateMarker("Hand Ray Origin", 0.035f);
            reticle = CreateMarker("Hand Ray Reticle", 0.05f);
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
            var hasTrackedPose = UpdatePoseFromHandTracking();
            SetRayVisible(hasTrackedPose);
            if (hasTrackedPose)
            {
                UpdateRay();
            }
        }

        private bool UpdatePoseFromHandTracking()
        {
            if (!XRHandInput.TryGetPose(hand, out var position, out var rotation))
            {
                return false;
            }

            transform.localPosition = position;
            transform.localRotation = rotation;
            return true;
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

                var selectPressed = IsSelectPressed();
                var pressedThisFrame = selectPressed && !wasSelectPressed;
                wasSelectPressed = selectPressed;

                if (pressedThisFrame && hit.collider.TryGetComponent(out XRRayButton button))
                {
                    button.Press();
                }
            }
            else
            {
                wasSelectPressed = IsSelectPressed();
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
                reticle.localScale = Vector3.one * (color == hitColor ? 0.06f : 0.045f);
            }
        }

        private bool IsSelectPressed()
        {
            return XRHandInput.IsPinching(hand);
        }

        private void ApplyInitialFallbackPosition()
        {
            var xOffset = hand == XRHandedness.Left ? -0.24f : 0.24f;
            transform.localPosition = new Vector3(xOffset, 1.25f, 0.35f);
            transform.localRotation = Quaternion.identity;
        }

        private void SetRayVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }

            if (originMarker != null)
            {
                originMarker.gameObject.SetActive(visible);
            }

            if (reticle != null)
            {
                reticle.gameObject.SetActive(visible);
            }
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
