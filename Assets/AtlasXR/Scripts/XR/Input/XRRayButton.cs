using System;
using UnityEngine;

namespace AtlasXR.XR.Input
{
    [DisallowMultipleComponent]
    public sealed class XRRayButton : MonoBehaviour
    {
        public event Action Pressed;

        public void Press()
        {
            Pressed?.Invoke();
        }
    }
}
