using System;
using System.Text;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Scenarios.Runtime;
using AtlasXR.XR.Input;
using UnityEngine;
using UnityEngine.UI;

namespace AtlasXR.App.Bootstrap
{
    public sealed class XRProcedurePanel : MonoBehaviour
    {
        private const float PanelWidth = 900f;
        private const float PanelHeight = 560f;
        private const float WorldScale = 0.0012f;

        private ProcedureEngine procedureEngine;
        private ILearningScenarioService scenarioService;
        private IAgentService agentService;
        private IAgentToolExecutor toolExecutor;
        private Text titleText;
        private Text bodyText;
        private Font uiFont;
        private string status = "Loading scenario...";

        public Transform FollowTarget { get; set; }

        private void Start()
        {
            ResolveServices();
            BuildPanel();
            Refresh();
        }

        private void LateUpdate()
        {
            var target = FollowTarget != null ? FollowTarget : Camera.main?.transform;
            if (target == null)
            {
                return;
            }

            transform.position = target.position + target.forward * 1.35f + Vector3.down * 0.15f;
            transform.rotation = Quaternion.LookRotation(transform.position - target.position, Vector3.up);

        }

        private void ResolveServices()
        {
            if (AtlasXRBootstrapper.Services == null)
            {
                status = "AtlasXR services are not ready.";
                return;
            }

            procedureEngine = AtlasXRBootstrapper.Services.Resolve<ProcedureEngine>();
            AtlasXRBootstrapper.Services.TryResolve(out scenarioService);
            agentService = AtlasXRBootstrapper.Services.Resolve<IAgentService>();
            toolExecutor = AtlasXRBootstrapper.Services.Resolve<IAgentToolExecutor>();
        }

        private void BuildPanel()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                     Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObject = new GameObject("Procedure World Space Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localScale = Vector3.one * WorldScale;

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 12f;

            var background = CreateImage("Panel Background", canvasRect, Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            background.color = new Color(0.06f, 0.07f, 0.075f, 0.96f);

            titleText = CreateText("Title", canvasRect, new Vector2(0f, 214f), new Vector2(800f, 70f), 42, TextAnchor.MiddleLeft);
            bodyText = CreateText("Body", canvasRect, new Vector2(0f, 40f), new Vector2(800f, 265f), 27, TextAnchor.UpperLeft);

            CreateButton(canvasRect, "Start", new Vector2(-360f, -218f), StartProcedure);
            CreateButton(canvasRect, "Next", new Vector2(-180f, -218f), NextStep);
            CreateButton(canvasRect, "Back", new Vector2(0f, -218f), BackStep);
            CreateButton(canvasRect, "Reset", new Vector2(180f, -218f), ResetProcedure);
            CreateButton(canvasRect, "Ask", new Vector2(360f, -218f), AskCurrentStep);
        }

        private Text CreateText(
            string name,
            RectTransform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor anchor)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            return text;
        }

        private void CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, Action action)
        {
            var buttonObject = new GameObject($"{label} Button");
            buttonObject.name = $"{label} Button";
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(150f, 62f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.33f, 0.38f, 1f);

            var collider = buttonObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 8f);

            var rayButton = buttonObject.AddComponent<XRRayButton>();
            rayButton.Pressed += action;

            var text = CreateText($"{label} Label", rect, Vector2.zero, rect.sizeDelta, 25, TextAnchor.MiddleCenter);
            text.text = label;
        }

        private Image CreateImage(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return imageObject.AddComponent<Image>();
        }

        private void StartProcedure()
        {
            var procedure = GetProcedure();
            if (procedure == null)
            {
                status = "No procedure loaded.";
                Refresh();
                return;
            }

            procedureEngine.Start(procedure);
            status = "Procedure started.";
            Refresh();
        }

        private void NextStep()
        {
            TryRun(() =>
            {
                procedureEngine.CompleteCurrentStep();
                status = procedureEngine.Progress.State == ProcedureEngineState.Completed
                    ? "Procedure completed."
                    : "Advanced to next step.";
            });
        }

        private void BackStep()
        {
            TryRun(() =>
            {
                procedureEngine.MovePrevious();
                status = "Moved to previous step.";
            });
        }

        private void ResetProcedure()
        {
            procedureEngine.Reset();
            status = "Procedure reset.";
            Refresh();
        }

        private async void AskCurrentStep()
        {
            var procedure = GetProcedure();
            if (procedure == null || agentService == null || toolExecutor == null)
            {
                status = "Agent services are not ready.";
                Refresh();
                return;
            }

            if (procedureEngine.Progress.State == ProcedureEngineState.Idle)
            {
                procedureEngine.Start(procedure);
            }

            status = "Agent is thinking...";
            Refresh();

            try
            {
                var response = await agentService.HandleCommandAsync(
                    "What should I do next?",
                    procedure,
                    procedureEngine.Progress.CurrentStepIndex,
                    default);

                var results = toolExecutor.ExecuteAll(response.toolCalls);
                var builder = new StringBuilder(response.assistantMessage);
                foreach (var result in results)
                {
                    builder.AppendLine();
                    builder.Append(result.success ? "Tool: " : "Tool failed: ");
                    builder.Append(result.message);
                }

                status = builder.ToString();
            }
            catch (Exception exception)
            {
                status = exception.Message;
            }

            Refresh();
        }

        private ProcedureDefinition GetProcedure()
        {
            return scenarioService?.CurrentProcedure ?? procedureEngine?.CurrentProcedure;
        }

        private void TryRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                status = exception.Message;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (titleText == null || bodyText == null)
            {
                return;
            }

            var procedure = GetProcedure();
            titleText.text = procedure == null ? "AtlasXR" : procedure.title;

            var progress = procedureEngine?.Progress;
            if (progress == null || !progress.Value.HasCurrentStep)
            {
                bodyText.text = $"{status}\n\nAim a Quest controller ray at Start.";
                return;
            }

            var step = progress.Value.CurrentStep;
            bodyText.text =
                $"{status}\n\n" +
                $"Step {progress.Value.CurrentStepIndex + 1} of {progress.Value.TotalSteps}: {step.title}\n" +
                $"{step.instruction}\n\n" +
                $"{step.confirmationPrompt}";
        }
    }
}
