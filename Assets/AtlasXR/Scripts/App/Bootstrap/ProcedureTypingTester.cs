using System.Text;
using System.Threading;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Scenarios.Runtime;
using UnityEngine;

namespace AtlasXR.App.Bootstrap
{
    public sealed class ProcedureTypingTester : MonoBehaviour
    {
        [SerializeField] private TextAsset procedureJson;

        private ProcedureEngine procedureEngine;
        private ILearningScenarioService learningScenarioService;
        private IAgentService agentService;
        private IAgentToolExecutor agentToolExecutor;
        private IAtlasLogger logger;
        private ProcedureDefinition procedure;
        private string command = "start";
        private string status = "Type 'start' and press Enter.";
        private Vector2 scrollPosition;

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
            AtlasXRBootstrapper.Services.TryResolve(out logger);
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
                        break;
                    case "next":
                        procedureEngine.CompleteCurrentStep();
                        status = procedureEngine.Progress.State == ProcedureEngineState.Completed
                            ? "Procedure completed."
                            : "Advanced to next step.";
                        break;
                    case "back":
                        procedureEngine.MovePrevious();
                        status = "Moved to previous step.";
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
