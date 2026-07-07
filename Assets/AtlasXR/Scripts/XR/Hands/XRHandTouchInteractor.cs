using AtlasXR.XR.Input;
using UnityEngine;

namespace AtlasXR.XR.Hands
{
    [DisallowMultipleComponent]
    public sealed class XRHandTouchInteractor : MonoBehaviour
    {
        [SerializeField] private XRHandedness hand = XRHandedness.Right;
        [SerializeField] private float touchRadius = 0.018f;
        [SerializeField] private Color touchColor = new Color(1f, 0.78f, 0.15f, 0.9f);
        [SerializeField] private Color idleColor = new Color(0.35f, 1f, 0.62f, 0.75f);

        private readonly Collider[] overlaps = new Collider[8];
        private XRRayButton currentButton;
        private Transform fingertipMarker;
        private Material markerMaterial;

        public XRHandedness Hand
        {
            get => hand;
            set => hand = value;
        }

        private void Awake()
        {
            markerMaterial = CreateMarkerMaterial();
            fingertipMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            fingertipMarker.name = $"{name} Touch Fingertip";
            fingertipMarker.localScale = Vector3.one * (touchRadius * 2f);
            fingertipMarker.GetComponent<Renderer>().material = markerMaterial;

            var collider = fingertipMarker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void OnDestroy()
        {
            if (fingertipMarker != null)
            {
                Destroy(fingertipMarker.gameObject);
            }

            if (markerMaterial != null)
            {
                Destroy(markerMaterial);
            }
        }

        private void Update()
        {
            UpdateTouch();
        }

        private void UpdateTouch()
        {
            if (!XRHandInput.TryGetIndexTipPosition(hand, out var localTipPosition))
            {
                SetMarkerVisible(false);
                currentButton = null;
                return;
            }

            var tipPosition = transform.parent != null
                ? transform.parent.TransformPoint(localTipPosition)
                : localTipPosition;

            SetMarkerVisible(true);
            fingertipMarker.position = tipPosition;

            var touchedButton = FindTouchedButton(tipPosition);
            if (touchedButton != currentButton)
            {
                currentButton = touchedButton;
                currentButton?.Press();
            }

            SetMarkerColor(touchedButton != null ? touchColor : idleColor);
        }

        private XRRayButton FindTouchedButton(Vector3 tipPosition)
        {
            var overlapCount = Physics.OverlapSphereNonAlloc(
                tipPosition,
                touchRadius,
                overlaps,
                ~0,
                QueryTriggerInteraction.Collide);

            for (var index = 0; index < overlapCount; index++)
            {
                var hit = overlaps[index];
                if (hit != null && hit.TryGetComponent(out XRRayButton button))
                {
                    return button;
                }
            }

            return null;
        }

        private void SetMarkerVisible(bool visible)
        {
            if (fingertipMarker != null)
            {
                fingertipMarker.gameObject.SetActive(visible);
            }
        }

        private static Material CreateMarkerMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.renderQueue = 5000;
            return material;
        }

        private void SetMarkerColor(Color color)
        {
            if (markerMaterial == null)
            {
                return;
            }

            if (markerMaterial.HasProperty("_BaseColor"))
            {
                markerMaterial.SetColor("_BaseColor", color);
            }

            if (markerMaterial.HasProperty("_Color"))
            {
                markerMaterial.SetColor("_Color", color);
            }
        }
    }
}
