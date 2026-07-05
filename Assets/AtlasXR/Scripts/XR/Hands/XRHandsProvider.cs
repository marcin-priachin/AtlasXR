using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace AtlasXR.XR.Hands
{
    internal static class XRHandsProvider
    {
        private static readonly List<XRHandSubsystem> Subsystems = new List<XRHandSubsystem>();
        private static XRHandSubsystem currentSubsystem;
        private static readonly XRHandJointID[][] FingerJointIds =
        {
            new[]
            {
                XRHandJointID.ThumbMetacarpal,
                XRHandJointID.ThumbProximal,
                XRHandJointID.ThumbDistal,
                XRHandJointID.ThumbTip
            },
            new[]
            {
                XRHandJointID.IndexMetacarpal,
                XRHandJointID.IndexProximal,
                XRHandJointID.IndexIntermediate,
                XRHandJointID.IndexDistal,
                XRHandJointID.IndexTip
            },
            new[]
            {
                XRHandJointID.MiddleMetacarpal,
                XRHandJointID.MiddleProximal,
                XRHandJointID.MiddleIntermediate,
                XRHandJointID.MiddleDistal,
                XRHandJointID.MiddleTip
            },
            new[]
            {
                XRHandJointID.RingMetacarpal,
                XRHandJointID.RingProximal,
                XRHandJointID.RingIntermediate,
                XRHandJointID.RingDistal,
                XRHandJointID.RingTip
            },
            new[]
            {
                XRHandJointID.LittleMetacarpal,
                XRHandJointID.LittleProximal,
                XRHandJointID.LittleIntermediate,
                XRHandJointID.LittleDistal,
                XRHandJointID.LittleTip
            }
        };

        public static bool TryGetPose(XRHandedness hand, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;

            if (!TryGetHand(hand, out var xrHand) ||
                !TryGetJointPose(xrHand, XRHandJointID.Palm, out var pose) &&
                !TryGetJointPose(xrHand, XRHandJointID.Wrist, out pose))
            {
                return false;
            }

            position = pose.position;
            rotation = pose.rotation;
            return IsUsablePosition(position);
        }

        public static bool TryGetHandSkeleton(
            XRHandedness hand,
            out Vector3 rootPosition,
            out Quaternion rootRotation,
            List<Vector3>[] fingerPositions)
        {
            rootPosition = default;
            rootRotation = default;
            ClearFingerPositions(fingerPositions);

            if (!TryGetHand(hand, out var xrHand) ||
                !TryGetJointPose(xrHand, XRHandJointID.Palm, out var rootPose) &&
                !TryGetJointPose(xrHand, XRHandJointID.Wrist, out rootPose))
            {
                return false;
            }

            rootPosition = rootPose.position;
            rootRotation = rootPose.rotation;

            var foundFingerJoint = false;
            for (var fingerIndex = 0; fingerIndex < XRHandInput.FingerCount; fingerIndex++)
            {
                var positions = fingerPositions[fingerIndex];
                positions.Clear();

                var jointIds = FingerJointIds[fingerIndex];
                for (var jointIndex = 0; jointIndex < jointIds.Length; jointIndex++)
                {
                    if (TryGetJointPose(xrHand, jointIds[jointIndex], out var pose))
                    {
                        positions.Add(pose.position);
                        foundFingerJoint = true;
                    }
                }
            }

            return foundFingerJoint;
        }

        public static bool IsPinching(XRHandedness hand)
        {
            if (!TryGetHand(hand, out var xrHand) ||
                !TryGetJointPose(xrHand, XRHandJointID.ThumbTip, out var thumbTip) ||
                !TryGetJointPose(xrHand, XRHandJointID.IndexTip, out var indexTip))
            {
                return false;
            }

            return Vector3.Distance(thumbTip.position, indexTip.position) < 0.035f;
        }

        private static bool TryGetHand(XRHandedness hand, out XRHand xrHand)
        {
            var subsystem = GetRunningSubsystem();
            if (subsystem == null)
            {
                xrHand = default;
                return false;
            }

            xrHand = hand == XRHandedness.Left ? subsystem.leftHand : subsystem.rightHand;
            return xrHand.isTracked;
        }

        private static XRHandSubsystem GetRunningSubsystem()
        {
            if (currentSubsystem != null && currentSubsystem.running)
            {
                return currentSubsystem;
            }

            Subsystems.Clear();
            SubsystemManager.GetSubsystems(Subsystems);

            for (var index = 0; index < Subsystems.Count; index++)
            {
                var subsystem = Subsystems[index];
                if (subsystem != null && subsystem.running)
                {
                    currentSubsystem = subsystem;
                    return subsystem;
                }
            }

            currentSubsystem = null;
            return null;
        }

        private static bool TryGetJointPose(XRHand hand, XRHandJointID jointId, out Pose pose)
        {
            if (!hand.GetJoint(jointId).TryGetPose(out pose))
            {
                return false;
            }

            return IsUsablePosition(pose.position);
        }

        private static bool IsUsablePosition(Vector3 position)
        {
            return position.sqrMagnitude > 0.0001f;
        }

        private static void ClearFingerPositions(List<Vector3>[] fingerPositions)
        {
            for (var index = 0; index < fingerPositions.Length; index++)
            {
                fingerPositions[index].Clear();
            }
        }
    }
}
