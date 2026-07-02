using System.Collections.Generic;
using AtlasXR.Core.Logging;
using UnityEngine;

namespace AtlasXR.XR.Highlighting
{
    public sealed class HighlightService : IHighlightService
    {
        private readonly Dictionary<string, EquipmentComponent> componentsById = new Dictionary<string, EquipmentComponent>();
        private readonly IAtlasLogger logger;
        private EquipmentComponent highlightedComponent;

        public HighlightService(IAtlasLogger logger)
        {
            this.logger = logger;
            Active = this;
        }

        public static IHighlightService Active { get; private set; }

        public void Register(EquipmentComponent component)
        {
            if (component == null || string.IsNullOrWhiteSpace(component.ComponentId))
            {
                return;
            }

            componentsById[component.ComponentId] = component;
        }

        public void Unregister(EquipmentComponent component)
        {
            if (component == null || string.IsNullOrWhiteSpace(component.ComponentId))
            {
                return;
            }

            if (componentsById.TryGetValue(component.ComponentId, out var registeredComponent) &&
                registeredComponent == component)
            {
                componentsById.Remove(component.ComponentId);
            }

            if (highlightedComponent == component)
            {
                highlightedComponent.SetHighlighted(false);
                highlightedComponent = null;
            }
        }

        public bool HighlightComponent(string componentId)
        {
            if (string.IsNullOrWhiteSpace(componentId))
            {
                logger?.Warning("Cannot highlight equipment component because component id is empty.");
                return false;
            }

            var normalizedComponentId = componentId.Trim();
            if (!componentsById.TryGetValue(normalizedComponentId, out var component) || component == null)
            {
                RebuildSceneIndex();
            }

            if (!componentsById.TryGetValue(normalizedComponentId, out component) || component == null)
            {
                logger?.Warning($"No equipment component found with id '{normalizedComponentId}'.");
                return false;
            }

            ClearHighlight();
            component.SetHighlighted(true);
            highlightedComponent = component;
            logger?.Info($"Highlighted equipment component '{normalizedComponentId}'.");
            return true;
        }

        public void ClearHighlight()
        {
            if (highlightedComponent == null)
            {
                return;
            }

            highlightedComponent.SetHighlighted(false);
            highlightedComponent = null;
        }

        private void RebuildSceneIndex()
        {
            componentsById.Clear();
            var sceneComponents = Object.FindObjectsByType<EquipmentComponent>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (var component in sceneComponents)
            {
                Register(component);
            }
        }
    }
}
