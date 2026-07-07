using System.Collections.Generic;
using UnityEngine;

namespace AtlasXR.XR.Hands
{
    internal static class XRHandInput
    {
        public const int FingerCount = 5;

        public static bool TryGetPose(XRHandedness hand, out Vector3 position, out Quaternion rotation)
        {
            return XRHandsProvider.TryGetPose(hand, out position, out rotation);
        }

        public static bool TryGetIndexTipPosition(XRHandedness hand, out Vector3 position)
        {
            return XRHandsProvider.TryGetIndexTipPosition(hand, out position);
        }

        public static bool TryGetHandSkeleton(
            XRHandedness hand,
            out Vector3 rootPosition,
            out Quaternion rootRotation,
            List<Vector3>[] fingerPositions)
        {
            return XRHandsProvider.TryGetHandSkeleton(hand, out rootPosition, out rootRotation, fingerPositions);
        }

        public static bool IsPinching(XRHandedness hand)
        {
            return XRHandsProvider.IsPinching(hand);
        }
    }
}
