using System;
using System.Text;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Scenarios.Runtime;
using AtlasXR.Voice.SpeechToText;
using AtlasXR.XR.Input;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace AtlasXR.App.Bootstrap
{
    public sealed class XRProcedurePanel : MonoBehaviour
    {
        private const int VoiceSampleRate = 16000;
        private const int MaxVoiceSeconds = 8;
        private const float PanelWidth = 900f;
        private const float PanelHeight = 560f;
        private const float WorldScale = 0.0012f;

        private ProcedureEngine procedureEngine;
        private ILearningScenarioService scenarioService;
        private IAgentService agentService;
        private IAgentToolExecutor toolExecutor;
        private ISpeechToTextService speechToTextService;
        private Text titleText;
        private Text bodyText;
        private Text transcriptText;
        private Font uiFont;
        private string transcribedSpeech = "No speech transcribed yet.";
        private string status = "Loading scenario...";
        private AudioClip voiceRecording;
        private bool isRecordingVoice;

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
            AtlasXRBootstrapper.Services.TryResolve(out speechToTextService);
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
            bodyText = CreateText("Body", canvasRect, new Vector2(0f, 62f), new Vector2(800f, 220f), 27, TextAnchor.UpperLeft);
            transcriptText = CreateText("Transcript", canvasRect, new Vector2(0f, -125f), new Vector2(800f, 54f), 22, TextAnchor.UpperLeft);

            CreateButton(canvasRect, "Start", new Vector2(-360f, -218f), StartProcedure);
            CreateButton(canvasRect, "Next", new Vector2(-180f, -218f), NextStep);
            CreateButton(canvasRect, "Back", new Vector2(0f, -218f), BackStep);
            CreateButton(canvasRect, "Reset", new Vector2(180f, -218f), ResetProcedure);
            CreateButton(canvasRect, "Ask", new Vector2(360f, -218f), AskCurrentStep);
            CreateButton(canvasRect, "Voice", new Vector2(360f, -142f), ToggleVoiceCommand);
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

        public void SetTranscribedSpeech(string transcript)
        {
            transcribedSpeech = string.IsNullOrWhiteSpace(transcript)
                ? "No speech transcribed yet."
                : transcript.Trim();
            Refresh();
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
            await RunAgentCommand("What should I do next?");
        }

        private async System.Threading.Tasks.Task RunAgentCommand(string userCommand)
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
                    userCommand,
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

        private void ToggleVoiceCommand()
        {
            if (isRecordingVoice)
            {
                StopVoiceRecordingAndRun();
            }
            else
            {
                StartVoiceRecording();
            }
        }

        private void StartVoiceRecording()
        {
            if (speechToTextService == null)
            {
                status = "Speech-to-text service is not ready.";
                Refresh();
                return;
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                status = "No microphone device found.";
                Refresh();
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                status = "Microphone permission requested. Press Voice again after allowing it.";
                Refresh();
                return;
            }
#endif

            voiceRecording = Microphone.Start(null, false, MaxVoiceSeconds, VoiceSampleRate);
            isRecordingVoice = voiceRecording != null;
            status = isRecordingVoice ? "Listening... press Voice again to stop." : "Could not start microphone recording.";
            Refresh();
        }

        private async void StopVoiceRecordingAndRun()
        {
            if (!isRecordingVoice || voiceRecording == null)
            {
                return;
            }

            var samplePosition = Microphone.GetPosition(null);
            Microphone.End(null);
            isRecordingVoice = false;

            var recordedClip = TrimRecording(voiceRecording, samplePosition);
            voiceRecording = null;
            if (recordedClip == null)
            {
                status = "No voice audio was captured.";
                Refresh();
                return;
            }

            try
            {
                status = "Transcribing voice...";
                Refresh();

                var result = await speechToTextService.TranscribeAsync(
                    recordedClip,
                    "Maintenance assistant command",
                    default);

                SetTranscribedSpeech(result?.text);
                if (string.IsNullOrWhiteSpace(result?.text))
                {
                    status = "No speech was transcribed.";
                    Refresh();
                    return;
                }

                await RunAgentCommand(result.text.Trim());
            }
            catch (Exception exception)
            {
                status = exception.Message;
                Refresh();
            }
            finally
            {
                Destroy(recordedClip);
            }
        }

        private static AudioClip TrimRecording(AudioClip sourceClip, int samplePosition)
        {
            if (sourceClip == null)
            {
                return null;
            }

            var samplesPerChannel = samplePosition > 0 ? samplePosition : sourceClip.samples;
            samplesPerChannel = Mathf.Clamp(samplesPerChannel, 0, sourceClip.samples);
            if (samplesPerChannel <= 0)
            {
                return null;
            }

            var channels = sourceClip.channels;
            var samples = new float[samplesPerChannel * channels];
            sourceClip.GetData(samples, 0);

            var trimmedClip = AudioClip.Create("Voice Command", samplesPerChannel, channels, sourceClip.frequency, false);
            trimmedClip.SetData(samples, 0);
            return trimmedClip;
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
            if (titleText == null || bodyText == null || transcriptText == null)
            {
                return;
            }

            var procedure = GetProcedure();
            titleText.text = procedure == null ? "AtlasXR" : procedure.title;
            transcriptText.text = $"Heard: {transcribedSpeech}";

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
