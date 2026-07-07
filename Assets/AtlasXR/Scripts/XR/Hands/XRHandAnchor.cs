using UnityEngine;

namespace AtlasXR.XR.Hands
{
    [DisallowMultipleComponent]
    public sealed class XRHandAnchor : MonoBehaviour
    {
        [SerializeField] private XRHandedness hand = XRHandedness.Left;

        public XRHandedness Hand
        {
            get => hand;
            set => hand = value;
        }

        private void Update()
        {
            UpdatePose();
        }

        private void UpdatePose()
        {
            if (!XRHandInput.TryGetPose(hand, out var position, out var rotation))
            {
                return;
            }

            transform.localPosition = position;
            transform.localRotation = rotation;
        }
    }
}
