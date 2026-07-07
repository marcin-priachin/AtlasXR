using System;
using System.Text;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Scenarios.Runtime;
using AtlasXR.Voice.SpeechToText;
using AtlasXR.Voice.TextToSpeech;
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
        private ITextToSpeechService textToSpeechService;
        private Text titleText;
        private Text bodyText;
        private Text transcriptText;
        private Text askButtonText;
        private Transform canvasRoot;
        private Transform cachedMainCamera;
        private Font uiFont;
        private string transcribedSpeech = "No speech transcribed yet.";
        private string status = "Loading scenario...";
        private AudioClip voiceRecording;
        private AudioSource speechAudioSource;
        private AudioClip currentSpokenClip;
        private int lastSpokenStepIndex = -1;
        private bool isRecordingVoice;
        private bool spokenOutputEnabled = true;

        public Transform FollowTarget { get; set; }
        public Transform ViewerTarget { get; set; }
        public bool AttachToFollowTarget { get; set; }
        public Vector3 AttachedLocalPosition { get; set; } = new Vector3(0f, 0.12f, 0.04f);
        public float PanelScale
        {
            get => panelScale;
            set
            {
                panelScale = Mathf.Max(0.1f, value);
                ApplyPanelScale();
            }
        }

        private float panelScale = 1f;

        private void Start()
        {
            ResolveServices();
            BuildPanel();
            Refresh();
        }

        private void LateUpdate()
        {
            var target = FollowTarget != null ? FollowTarget : GetMainCameraTransform();
            if (target == null)
            {
                return;
            }

            if (AttachToFollowTarget)
            {
                transform.position = target.TransformPoint(AttachedLocalPosition);
                FaceViewer();
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
            AtlasXRBootstrapper.Services.TryResolve(out textToSpeechService);
            EnsureSpeechAudioSource();
        }

        private void BuildPanel()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                     Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObject = new GameObject("Procedure World Space Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvasRoot = canvasObject.transform;
            ApplyPanelScale();

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
            transcriptText = CreateText("Transcript", canvasRect, new Vector2(0f, -125f), new Vector2(800f, 72f), 20, TextAnchor.UpperLeft);

            CreateButton(canvasRect, "Start", new Vector2(-360f, -218f), StartProcedure);
            CreateButton(canvasRect, "Next", new Vector2(-180f, -218f), NextStep);
            CreateButton(canvasRect, "Back", new Vector2(0f, -218f), BackStep);
            CreateButton(canvasRect, "Reset", new Vector2(180f, -218f), ResetProcedure);
            askButtonText = CreateButton(canvasRect, "Ask", new Vector2(360f, -218f), ToggleAskVoiceCommand);
            CreateButton(canvasRect, "Speak", new Vector2(180f, -142f), ToggleSpokenOutput);
        }

        private void ApplyPanelScale()
        {
            if (canvasRoot != null)
            {
                canvasRoot.localScale = Vector3.one * (WorldScale * panelScale);
            }
        }

        private void FaceViewer()
        {
            var viewer = ViewerTarget != null ? ViewerTarget : GetMainCameraTransform();
            if (viewer == null)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(transform.position - viewer.position, Vector3.up);
        }

        private Transform GetMainCameraTransform()
        {
            if (cachedMainCamera == null && Camera.main != null)
            {
                cachedMainCamera = Camera.main.transform;
            }

            return cachedMainCamera;
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

        private Text CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, Action action)
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
            return text;
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
            SpeakCurrentStepInstruction();
        }

        private void NextStep()
        {
            TryRun(() =>
            {
                procedureEngine.CompleteCurrentStep();
                status = procedureEngine.Progress.State == ProcedureEngineState.Completed
                    ? "Procedure completed."
                    : "Advanced to next step.";
                SpeakCurrentStepInstruction();
            });
        }

        private void BackStep()
        {
            TryRun(() =>
            {
                procedureEngine.MovePrevious();
                status = "Moved to previous step.";
                SpeakCurrentStepInstruction();
            });
        }

        private void ResetProcedure()
        {
            procedureEngine.Reset();
            status = "Procedure reset.";
            Refresh();
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
                SpeakText(response.assistantMessage);
            }
            catch (Exception exception)
            {
                status = exception.Message;
            }

            Refresh();
        }

        private void ToggleSpokenOutput()
        {
            spokenOutputEnabled = !spokenOutputEnabled;
            status = spokenOutputEnabled ? "Spoken output enabled." : "Spoken output muted.";
            Refresh();
        }

        private void SpeakCurrentStepInstruction()
        {
            if (procedureEngine == null || !procedureEngine.Progress.HasCurrentStep)
            {
                return;
            }

            var progress = procedureEngine.Progress;
            if (progress.CurrentStepIndex == lastSpokenStepIndex)
            {
                return;
            }

            lastSpokenStepIndex = progress.CurrentStepIndex;
            var step = progress.CurrentStep;
            SpeakText($"{step.title}. {step.instruction}");
        }

        private async void SpeakText(string text)
        {
            if (!spokenOutputEnabled || textToSpeechService == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var clip = await textToSpeechService.SynthesizeClipAsync(
                    new TextToSpeechRequest
                    {
                        text = text.Trim(),
                        instructions = "Speak clearly and concisely as an XR maintenance assistant."
                    },
                    default);

                PlaySpeechClip(clip);
            }
            catch (Exception exception)
            {
                status = $"Text-to-speech failed: {exception.Message}";
                Refresh();
            }
        }

        private void PlaySpeechClip(AudioClip clip)
        {
            EnsureSpeechAudioSource();
            if (clip == null || speechAudioSource == null)
            {
                status = "Speech audio could not play.";
                Refresh();
                return;
            }

            EnsureAudioOutputReady();
            speechAudioSource.Stop();
            if (currentSpokenClip != null)
            {
                Destroy(currentSpokenClip);
            }

            currentSpokenClip = clip;
            speechAudioSource.clip = clip;
            speechAudioSource.volume = 1f;
            speechAudioSource.Play();
        }

        private void ConfigureSpeechAudioSource()
        {
            if (speechAudioSource == null)
            {
                return;
            }

            speechAudioSource.playOnAwake = false;
            speechAudioSource.loop = false;
            speechAudioSource.spatialBlend = 0f;
            speechAudioSource.volume = 1f;
            speechAudioSource.mute = false;
            speechAudioSource.enabled = true;
            speechAudioSource.ignoreListenerPause = true;
        }

        private static void EnsureAudioOutputReady()
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            var listener = FindFirstObjectByType<AudioListener>();
            if (listener == null && Camera.main != null)
            {
                listener = Camera.main.gameObject.AddComponent<AudioListener>();
            }

            if (listener != null)
            {
                listener.enabled = true;
            }
        }

        private void EnsureSpeechAudioSource()
        {
            if (speechAudioSource == null)
            {
                var audioObject = GameObject.Find("AtlasXR Runtime Audio");
                if (audioObject == null)
                {
                    audioObject = new GameObject("AtlasXR Runtime Audio");
                    DontDestroyOnLoad(audioObject);
                }

                speechAudioSource = audioObject.GetComponent<AudioSource>();
                if (speechAudioSource == null)
                {
                    speechAudioSource = audioObject.AddComponent<AudioSource>();
                }
            }

            ConfigureSpeechAudioSource();
        }

        private void ToggleAskVoiceCommand()
        {
            if (isRecordingVoice)
            {
                StopVoiceRecordingAndAsk();
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
                status = "Microphone permission requested. Press Ask again after allowing it.";
                Refresh();
                return;
            }
#endif

            voiceRecording = Microphone.Start(null, false, MaxVoiceSeconds, VoiceSampleRate);
            isRecordingVoice = voiceRecording != null;
            SetAskButtonRecordingState(isRecordingVoice);
            status = isRecordingVoice ? "Listening... press Ask again to send." : "Could not start microphone recording.";
            Refresh();
        }

        private async void StopVoiceRecordingAndAsk()
        {
            if (!isRecordingVoice || voiceRecording == null)
            {
                return;
            }

            var samplePosition = Microphone.GetPosition(null);
            Microphone.End(null);
            isRecordingVoice = false;
            SetAskButtonRecordingState(false);

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
                status = "Transcribing question...";
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

        private void SetAskButtonRecordingState(bool recording)
        {
            if (askButtonText != null)
            {
                askButtonText.text = recording ? "Send" : "Ask";
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
                bodyText.text = AttachToFollowTarget
                    ? $"{status}\n\nTouch Start with your right index finger."
                    : $"{status}\n\nAim a Quest controller ray at Start.";
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
