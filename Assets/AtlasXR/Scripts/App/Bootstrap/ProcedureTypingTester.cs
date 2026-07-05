using System.Text;
using System.Threading;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Scenarios.Runtime;
using AtlasXR.Voice.SpeechToText;
using AtlasXR.Voice.TextToSpeech;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace AtlasXR.App.Bootstrap
{
    public sealed class ProcedureTypingTester : MonoBehaviour
    {
        private const int VoiceSampleRate = 16000;
        private const int MaxVoiceSeconds = 8;

        [SerializeField] private TextAsset procedureJson;

        private ProcedureEngine procedureEngine;
        private ILearningScenarioService learningScenarioService;
        private IAgentService agentService;
        private IAgentToolExecutor agentToolExecutor;
        private ISpeechToTextService speechToTextService;
        private ITextToSpeechService textToSpeechService;
        private IAtlasLogger logger;
        private ProcedureDefinition procedure;
        private string command = "start";
        private string transcribedSpeech = "No speech transcribed yet.";
        private string status = "Type 'start' and press Enter.";
        private Vector2 scrollPosition;
        private AudioClip voiceRecording;
        private AudioSource speechAudioSource;
        private AudioClip currentSpokenClip;
        private int lastSpokenStepIndex = -1;
        private bool isRecordingVoice;
        private bool spokenOutputEnabled = true;

        private void Start()
        {
            if (AtlasXRBootstrapper.Services == null)
            {
                status = "AtlasXR services are not ready.";
                return;
            }

            procedureEngine = AtlasXRBootstrapper.Services.Resolve<ProcedureEngine>();
            AtlasXRBootstrapper.Services.TryResolve(out learningScenarioService);
            agentService = AtlasXRBootstrapper.Services.Resolve<IAgentService>();
            agentToolExecutor = AtlasXRBootstrapper.Services.Resolve<IAgentToolExecutor>();
            AtlasXRBootstrapper.Services.TryResolve(out speechToTextService);
            AtlasXRBootstrapper.Services.TryResolve(out textToSpeechService);
            AtlasXRBootstrapper.Services.TryResolve(out logger);
            EnsureSpeechAudioSource();
            LoadProcedure();
        }

        private void OnGUI()
        {
            const int panelWidth = 520;
            GUILayout.BeginArea(new Rect(16, 16, panelWidth, Screen.height - 32), GUI.skin.box);
            GUILayout.Label("AtlasXR Procedure Typing Test");
            GUILayout.Label("Commands: start, next, back, reset");

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ProcedureCommand");
            command = GUILayout.TextField(command);
            if (GUILayout.Button("Run", GUILayout.Width(72)))
            {
                ExecuteCommand();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Transcribed speech");
            transcribedSpeech = GUILayout.TextField(transcribedSpeech);
            if (GUILayout.Button("Use Transcript As Command"))
            {
                command = transcribedSpeech;
            }

            if (GUILayout.Button(isRecordingVoice ? "Stop Voice Command" : "Record Voice Command"))
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

            spokenOutputEnabled = GUILayout.Toggle(spokenOutputEnabled, "Speak instructions and agent answers");

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown &&
                currentEvent.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "ProcedureCommand")
            {
                ExecuteCommand();
                currentEvent.Use();
            }

            GUILayout.Space(8);
            GUILayout.Label(status);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            DrawProcedure();
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        public void SetTranscribedSpeech(string transcript, bool copyToCommand)
        {
            transcribedSpeech = string.IsNullOrWhiteSpace(transcript)
                ? "No speech transcribed yet."
                : transcript.Trim();

            if (copyToCommand && !string.IsNullOrWhiteSpace(transcript))
            {
                command = transcript.Trim();
            }
        }

        private void LoadProcedure()
        {
            if (procedureJson == null)
            {
                procedure = learningScenarioService?.CurrentProcedure;
                status = procedure == null
                    ? "Assign a procedure JSON or load a learning scenario."
                    : $"Using scenario procedure '{procedure.id}'.";
                return;
            }

            procedure = procedureEngine.LoadFromJson(procedureJson.text);
            status = $"Loaded '{procedure.id}'. Type 'start' to begin.";
        }

        private async void ExecuteCommand()
        {
            await ExecuteCommandAsync();
        }

        private async System.Threading.Tasks.Task ExecuteCommandAsync()
        {
            if (procedureEngine == null || procedure == null)
            {
                LoadProcedure();
                return;
            }

            var normalizedCommand = command.Trim().ToLowerInvariant();
            try
            {
                switch (normalizedCommand)
                {
                    case "start":
                        procedureEngine.Start(procedure);
                        status = "Procedure started.";
                        SpeakCurrentStepInstruction();
                        break;
                    case "next":
                        procedureEngine.CompleteCurrentStep();
                        status = procedureEngine.Progress.State == ProcedureEngineState.Completed
                            ? "Procedure completed."
                            : "Advanced to next step.";
                        SpeakCurrentStepInstruction();
                        break;
                    case "back":
                        procedureEngine.MovePrevious();
                        status = "Moved to previous step.";
                        SpeakCurrentStepInstruction();
                        break;
                    case "reset":
                        procedureEngine.Reset();
                        status = "Procedure reset. Type 'start' to begin again.";
                        break;
                    default:
                        await ExecuteAgentCommand(command);
                        break;
                }
            }
            catch (System.Exception exception)
            {
                status = exception.Message;
                logger?.Warning(exception.Message);
            }
        }

        private void StartVoiceRecording()
        {
            if (speechToTextService == null)
            {
                status = "Speech-to-text service is not ready.";
                return;
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                status = "No microphone device found.";
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                status = "Microphone permission requested. Press voice again after allowing it.";
                return;
            }
#endif

            voiceRecording = Microphone.Start(null, false, MaxVoiceSeconds, VoiceSampleRate);
            isRecordingVoice = voiceRecording != null;
            status = isRecordingVoice ? "Listening..." : "Could not start microphone recording.";
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
                return;
            }

            try
            {
                status = "Transcribing voice...";
                var result = await speechToTextService.TranscribeAsync(
                    recordedClip,
                    "Maintenance assistant command",
                    CancellationToken.None);

                SetTranscribedSpeech(result?.text, true);
                if (string.IsNullOrWhiteSpace(result?.text))
                {
                    status = "No speech was transcribed.";
                    return;
                }

                await ExecuteCommandAsync();
            }
            catch (System.Exception exception)
            {
                status = exception.Message;
                logger?.Warning(exception.Message);
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

        private async System.Threading.Tasks.Task ExecuteAgentCommand(string userCommand)
        {
            if (agentService == null || agentToolExecutor == null)
            {
                status = "Agent services are not ready.";
                return;
            }

            if (procedureEngine.Progress.State == ProcedureEngineState.Idle)
            {
                procedureEngine.Start(procedure);
            }

            status = "Agent is thinking...";
            var response = await agentService.HandleCommandAsync(
                userCommand,
                procedure,
                procedureEngine.Progress.CurrentStepIndex,
                CancellationToken.None);

            var results = agentToolExecutor.ExecuteAll(response.toolCalls);
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
                    CancellationToken.None);

                PlaySpeechClip(clip);
            }
            catch (System.Exception exception)
            {
                logger?.Warning($"Text-to-speech failed. {exception.Message}");
            }
        }

        private void PlaySpeechClip(AudioClip clip)
        {
            EnsureSpeechAudioSource();
            if (clip == null || speechAudioSource == null)
            {
                logger?.Warning("Speech audio could not play because the clip or audio source was unavailable.");
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

        private void DrawProcedure()
        {
            if (procedure == null)
            {
                GUILayout.Label("No procedure loaded.");
                return;
            }

            GUILayout.Label(procedure.title);
            GUILayout.Label(procedure.description);
            GUILayout.Label($"Estimated duration: {procedure.estimatedDurationMinutes} minutes");

            var progress = procedureEngine?.Progress;
            if (progress == null || !progress.Value.HasCurrentStep)
            {
                GUILayout.Label("No active step.");
                return;
            }

            var step = progress.Value.CurrentStep;
            GUILayout.Space(8);
            GUILayout.Label($"Step {progress.Value.CurrentStepIndex + 1} of {progress.Value.TotalSteps}: {step.title}");
            GUILayout.Label(step.instruction);
            GUILayout.Label(step.confirmationPrompt);

            if (!string.IsNullOrWhiteSpace(step.expectedOutcome))
            {
                GUILayout.Label($"Expected: {step.expectedOutcome}");
            }

            if (step.safetyNotes != null && step.safetyNotes.Count > 0)
            {
                GUILayout.Space(8);
                GUILayout.Label("Safety");
                foreach (var safetyNote in step.safetyNotes)
                {
                    GUILayout.Label($"- {safetyNote}");
                }
            }
        }
    }
}
