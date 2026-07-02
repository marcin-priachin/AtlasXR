using System;
using AtlasXR.Core.Events;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Runtime;
using AtlasXR.XR.Highlighting;

namespace AtlasXR.App.Bootstrap
{
    public sealed class ProcedureHighlightToolBridge : IDisposable
    {
        private const string HighlightComponentPrefix = "HighlightComponent:";
        private const string ClearHighlightToolId = "ClearHighlight";

        private readonly IHighlightService highlightService;
        private readonly IAtlasLogger logger;
        private readonly IDisposable stepChangedSubscription;
        private readonly IDisposable completedSubscription;

        public ProcedureHighlightToolBridge(
            IEventBroker eventBroker,
            IHighlightService highlightService,
            IAtlasLogger logger)
        {
            this.highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
            this.logger = logger;

            if (eventBroker == null)
            {
                throw new ArgumentNullException(nameof(eventBroker));
            }

            stepChangedSubscription = eventBroker.Subscribe<ProcedureStepChangedEvent>(OnProcedureStepChanged);
            completedSubscription = eventBroker.Subscribe<ProcedureCompletedEvent>(OnProcedureCompleted);
        }

        public void Dispose()
        {
            stepChangedSubscription.Dispose();
            completedSubscription.Dispose();
        }

        private void OnProcedureStepChanged(ProcedureStepChangedEvent eventData)
        {
            var toolIds = eventData.Step?.toolIds;
            if (toolIds == null || toolIds.Count == 0)
            {
                highlightService.ClearHighlight();
                return;
            }

            var highlighted = false;
            foreach (var toolId in toolIds)
            {
                if (string.IsNullOrWhiteSpace(toolId))
                {
                    continue;
                }

                if (string.Equals(toolId, ClearHighlightToolId, StringComparison.OrdinalIgnoreCase))
                {
                    highlightService.ClearHighlight();
                    highlighted = false;
                    continue;
                }

                if (!toolId.StartsWith(HighlightComponentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var componentId = toolId.Substring(HighlightComponentPrefix.Length).Trim();
                highlighted = highlightService.HighlightComponent(componentId) || highlighted;
            }

            if (!highlighted)
            {
                logger?.Info($"No highlight tool executed for procedure step '{eventData.Step?.id}'.");
            }
        }

        private void OnProcedureCompleted(ProcedureCompletedEvent eventData)
        {
            highlightService.ClearHighlight();
        }
    }
}
