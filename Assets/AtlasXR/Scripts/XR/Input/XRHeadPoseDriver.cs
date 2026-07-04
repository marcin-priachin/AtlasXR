using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace AtlasXR.XR.Input
{
    [DisallowMultipleComponent]
    public sealed class XRHeadPoseDriver : MonoBehaviour
    {
        private readonly List<InputDevice> devices = new List<InputDevice>();
        private InputDevice headDevice;
        private Vector3 fallbackPosition;

        private void Awake()
        {
            fallbackPosition = transform.position;
        }

        private void Update()
        {
            EnsureDevice();
            if (!headDevice.isValid)
            {
                transform.position = fallbackPosition;
                return;
            }

            if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var position))
            {
                transform.localPosition = position;
            }

            if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation))
            {
                transform.localRotation = rotation;
            }
        }

        private void EnsureDevice()
        {
            if (headDevice.isValid)
            {
                return;
            }

            devices.Clear();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
            if (devices.Count > 0)
            {
                headDevice = devices[0];
            }
        }
    }
}
