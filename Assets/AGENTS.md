# AGENTS.md

## Project Identity

This is a Unity XR portfolio project: **Enterprise XR Maintenance Assistant**.

The goal is to demonstrate production-quality architecture for an enterprise XR application, not to create a toy demo or game prototype.

The application runs on Meta Quest, starting with Quest 2 compatibility, using Unity, OpenXR, passthrough where available, hand/controller interaction, voice commands, AI-assisted guidance, and data-driven maintenance procedures.

The project should show that the developer can architect and implement a real-world XR product with clean systems, maintainable code, and extensible modules.

---

## High-Level Product Concept

A technician uses an XR headset to complete a guided maintenance procedure.

Example scenario:

* User selects a machine.
* User chooses a procedure, such as replacing a filter.
* The app loads procedure data.
* The system highlights the relevant component.
* The assistant explains each step.
* User can ask voice questions.
* AI can trigger in-app actions such as highlighting objects, repeating instructions, moving to the next step, or showing warnings.
* Completion data is recorded for analytics.

The specific procedure is only a sample. The architecture must support adding new equipment and procedures through data, not hardcoded logic.

---

## Technical Priorities

Prioritize:

1. Clean architecture.
2. Readable, maintainable C#.
3. Strong separation between systems.
4. Data-driven procedures.
5. XR interaction abstraction.
6. AI tool-call integration.
7. Testable non-Unity logic where practical.
8. Clear documentation.

Do not prioritize:

* Complex art.
* Over-polished visuals.
* Large amounts of content.
* Game-like mechanics.
* Hardcoded demo logic.
* Clever but obscure code.

---

## Unity Version and Packages

Target Unity 6 or newer unless the project explicitly uses another version.

Preferred XR stack:

* OpenXR
* XR Interaction Toolkit
* Meta XR features where needed
* Input System
* Addressables for remote/loadable content

Use Quest 2 as baseline hardware.

Quest 2 passthrough is acceptable for demonstrating architecture and interaction, but the app should be designed so it can benefit from Quest 3 or newer hardware without major rewrites.

---

## Folder Structure

Use this structure unless there is a strong reason to change it:

Assets/
AtlasXR/
Scripts/
App/
Bootstrap/
States/
Core/
DI/
Events/
Logging/
Services/
StateMachine/
XR/
Input/
Hands/
Passthrough/
Interaction/
Highlighting/
Procedures/
Runtime/
Data/
Validation/
AI/
Runtime/
Tools/
Prompts/
Voice/
SpeechToText/
TextToSpeech/
Backend/
Auth/
Api/
Analytics/
UI/
Views/
Presenters/
Shared/
Extensions/
Utilities/
ScriptableObjects/
Addressables/
Prefabs/
Scenes/
Docs/

---

## Architecture Rules

### 1. Use App States

Application flow should be controlled by a state machine.

Example states:

* BootState
* LoginState
* ProcedureSelectionState
* EquipmentLoadingState
* XrSessionState
* ProcedureStepState
* SummaryState
* ErrorState

States should coordinate systems, not contain heavy business logic.

---

### 2. Use Services for Capabilities

Major capabilities should be exposed through services.

Examples:

* IProcedureService
* IXrInteractionService
* IHighlightService
* IVoiceInputService
* ITextToSpeechService
* IAiAssistantService
* IBackendApiService
* IAnalyticsService
* IAddressableContentService
* IAuthenticationService

Services should be registered during bootstrap.

Avoid using global singletons unless explicitly justified.

---

### 3. Use Events for Decoupling

Use an event broker or event bus for cross-system communication.

Examples:

* ProcedureStartedEvent
* ProcedureStepChangedEvent
* ComponentHighlightedEvent
* VoiceCommandReceivedEvent
* AiToolRequestedEvent
* AnalyticsEventRecordedEvent
* SessionCompletedEvent

Systems should not directly depend on unrelated systems if an event would be cleaner.

---

### 4. Keep Procedures Data-Driven

Maintenance procedures must be stored as data.

Prefer ScriptableObjects for authoring and JSON-compatible DTOs for remote content.

A procedure should contain:

* Id
* Display name
* Equipment id
* Ordered steps
* Required components
* Safety warnings
* Completion conditions
* Optional AI context

Each step should contain:

* Step id
* Instruction text
* Target component id
* Optional highlight mode
* Optional animation id
* Optional voice prompt
* Optional validation rule

Avoid hardcoding procedure steps in MonoBehaviours.

---

### 5. AI Must Use Tools, Not Just Chat Text

The AI assistant should not only return text.

It should be able to request structured actions.

Example AI tools:

* HighlightComponent(componentId)
* ClearHighlight()
* GoToNextStep()
* RepeatCurrentStep()
* ShowSafetyWarning(warningId)
* PlayProcedureAnimation(animationId)
* MarkStepComplete(stepId)
* ExplainComponent(componentId)

AI output should be parsed into explicit tool requests before affecting Unity scene state.

Never allow raw AI text to directly control scene objects.

---

### 6. XR Layer Must Be Abstracted

XR input should not be scattered through the app.

Create a dedicated XR layer responsible for:

* Hand/controller input
* Ray interaction
* Grab/select interaction
* Gaze or pointer targeting
* Passthrough mode
* Component selection
* Spatial UI interaction

Business logic should not directly read low-level XR input.

---

### 7. Backend Should Be Replaceable

Backend integration should be abstracted.

Initial implementation may use mocked local data.

Design interfaces so the project can later support:

* Login
* Remote procedure download
* Analytics upload
* User progress
* Remote asset/content loading

Do not make gameplay/session logic depend directly on HTTP calls.

---

## Coding Standards

Use C#.

Prefer:

* Clear names.
* Small classes.
* Explicit dependencies.
* Interfaces for services.
* Constructor or initialization injection where practical.
* Async/await for IO-like operations.
* CancellationToken for async operations where practical.
* Immutable DTOs where reasonable.

Avoid:

* God classes.
* Large MonoBehaviours.
* Hidden dependencies.
* Magic strings.
* Hardcoded scene references.
* Business logic inside UI classes.
* Direct FindObjectOfType calls in production code.
* Uncontrolled static state.

---

## Naming Conventions

Interfaces start with `I`.

Examples:

* `IProcedureService`
* `IAiAssistantService`
* `IHighlightService`

Events end with `Event`.

Examples:

* `ProcedureStartedEvent`
* `VoiceCommandReceivedEvent`

App states end with `State`.

Examples:

* `BootState`
* `XrSessionState`

ScriptableObject definitions end with `Definition`.

Examples:

* `ProcedureDefinition`
* `EquipmentDefinition`
* `ComponentDefinition`

Runtime models use simple domain names.

Examples:

* `Procedure`
* `ProcedureStep`
* `EquipmentComponent`

---

## MonoBehaviour Rules

MonoBehaviours should mostly handle Unity-specific concerns:

* Scene references
* Unity lifecycle
* Input callbacks
* Visual behavior
* XR interaction components
* View binding

MonoBehaviours should delegate business logic to services or domain classes.

If a MonoBehaviour grows too large, extract logic into a plain C# class or service.

---

## UI Rules

Use a simple MVP-style split where practical:

* View: Unity UI references and display updates.
* Presenter/Controller: UI logic and service calls.
* Services: actual business logic.

UI should not directly manipulate procedure state unless through a service or app state.

---

## Error Handling

Handle errors explicitly.

Important failures should transition to ErrorState or show a user-readable error.

Examples:

* Procedure failed to load.
* Voice service unavailable.
* AI request failed.
* Required component missing.
* Backend unavailable.

For portfolio purposes, graceful fallback is more important than perfect production robustness.

---

## Logging

Use a project logging abstraction.

Avoid raw Debug.Log scattered everywhere.

Example:

* `ILogger`
* `UnityLogger`

Logs should include useful context:

* Current state
* Procedure id
* Step id
* Component id
* Service name

---

## Testing

Prioritize tests for non-Unity logic:

* Procedure validation.
* State transitions.
* AI tool parsing.
* Event broker behavior.
* DTO conversion.
* Completion rules.

Do not over-invest in complex Unity play mode tests unless they are clearly useful.

---

## Documentation Requirements

Important systems should have short documentation in `Assets/AtlasXR/Docs`.

Minimum documentation:

* ARCHITECTURE.md
* SETUP.md
* PROCEDURE_FORMAT.md
* AI_TOOLS.md

The README should explain:

* What the project demonstrates.
* Why the architecture is structured this way.
* How to run the project.
* Which parts are mocked.
* Which parts are designed for production extension.

---

## Portfolio Goal

Every technical decision should support the portfolio message:

“This developer can build and architect a production-quality enterprise XR application, not just a small Unity demo.”

When adding features, prefer features that demonstrate employable skills:

* OpenXR setup.
* Hand/controller interaction.
* Passthrough support.
* Voice commands.
* AI tool calls.
* Data-driven workflows.
* Backend abstraction.
* Analytics.
* Clean architecture.
* Documentation.

Avoid features that are visually fun but do not strengthen the hiring story.

---

## Implementation Style for Codex

When modifying the project:

1. Preserve the architecture.
2. Do not bypass services for quick fixes.
3. Do not introduce direct dependencies between unrelated modules.
4. Add interfaces before concrete implementations when the system is likely to need mocking or replacement.
5. Keep sample content separate from reusable framework code.
6. Update documentation when adding architectural concepts.
7. Prefer simple working vertical slices over large unfinished systems.
8. Make code easy for a senior reviewer to understand quickly.

---

## First Vertical Slice

The first useful version should include:

1. Boot state.
2. Mock login or local user profile.
3. Procedure selection.
4. Load one sample equipment model.
5. Start XR session.
6. Show one data-driven procedure.
7. Highlight target component.
8. Advance procedure steps.
9. Accept one voice or simulated voice command.
10. Trigger one AI-style tool call.
11. Record local analytics.
12. Show completion summary.

This vertical slice is more valuable than many disconnected experimental features.
