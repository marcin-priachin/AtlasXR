using System.Collections.Generic;
using UnityEngine;

namespace AtlasXR.XR.Highlighting
{
    [DisallowMultipleComponent]
    public sealed class EquipmentComponent : MonoBehaviour
    {
        [SerializeField] private string componentId;
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color highlightColor = new Color(1f, 0.78f, 0.1f, 1f);
        [SerializeField] private float emissionIntensity = 1.8f;

        private readonly List<MaterialState> materialStates = new List<MaterialState>();
        private bool isHighlighted;

        public string ComponentId => componentId;

        private void Reset()
        {
            componentId = gameObject.name;
            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }

            CacheMaterialStates();
        }

        private void OnEnable()
        {
            HighlightService.Active?.Register(this);
        }

        private void OnDisable()
        {
            HighlightService.Active?.Unregister(this);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted)
            {
                return;
            }

            if (materialStates.Count == 0)
            {
                CacheMaterialStates();
            }

            isHighlighted = highlighted;
            if (highlighted)
            {
                ApplyHighlight();
                return;
            }

            RestoreMaterials();
        }

        private void CacheMaterialStates()
        {
            materialStates.Clear();
            if (targetRenderers == null)
            {
                return;
            }

            foreach (var targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                var materials = targetRenderer.materials;
                foreach (var material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    materialStates.Add(new MaterialState(material));
                }
            }
        }

        private void ApplyHighlight()
        {
            foreach (var materialState in materialStates)
            {
                var material = materialState.Material;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", highlightColor);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", highlightColor);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                }
            }
        }

        private void RestoreMaterials()
        {
            foreach (var materialState in materialStates)
            {
                materialState.Restore();
            }
        }

        private readonly struct MaterialState
        {
            private readonly bool hadBaseColor;
            private readonly Color baseColor;
            private readonly bool hadColor;
            private readonly Color color;
            private readonly bool hadEmissionColor;
            private readonly Color emissionColor;
            private readonly bool emissionWasEnabled;

            public MaterialState(Material material)
            {
                Material = material;
                hadBaseColor = material.HasProperty("_BaseColor");
                baseColor = hadBaseColor ? material.GetColor("_BaseColor") : Color.white;
                hadColor = material.HasProperty("_Color");
                color = hadColor ? material.GetColor("_Color") : Color.white;
                hadEmissionColor = material.HasProperty("_EmissionColor");
                emissionColor = hadEmissionColor ? material.GetColor("_EmissionColor") : Color.black;
                emissionWasEnabled = material.IsKeywordEnabled("_EMISSION");
            }

            public Material Material { get; }

            public void Restore()
            {
                if (hadBaseColor)
                {
                    Material.SetColor("_BaseColor", baseColor);
                }

                if (hadColor)
                {
                    Material.SetColor("_Color", color);
                }

                if (hadEmissionColor)
                {
                    Material.SetColor("_EmissionColor", emissionColor);
                    if (emissionWasEnabled)
                    {
                        Material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        Material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
    }
}
