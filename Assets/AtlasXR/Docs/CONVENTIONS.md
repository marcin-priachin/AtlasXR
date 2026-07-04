# AtlasXR Conventions

## Purpose

This document defines how AtlasXR code should be written.

It is intentionally practical and focused on the current goal: building a working AI-powered XR maintenance assistant quickly without creating messy code.

## Core Principle

> Demo first. Architecture only where it directly supports the demo.

Do not build a general framework before the first vertical slice works.

## Engineering Values

AtlasXR values:

- Readability over cleverness.
- Explicit dependencies over hidden magic.
- Small classes over large manager objects.
- Data-driven procedures over hardcoded demo logic.
- Safe AI tool execution over raw AI control.
- Testable business logic where practical.
- Fast visible progress over theoretical completeness.

## Naming Conventions

Use clear, descriptive names.

Avoid vague names like:

- Manager
- Helper
- Utils
- Misc
- Controller, unless it is specifically controlling a UI/view flow

### Interfaces

Interfaces start with `I`.

Examples:

```csharp
IProcedureService
IAgentProvider
IHighlightService
IAnalyticsService
```

### Services

Concrete service implementations end with `Service`.

Examples:

```csharp
ProcedureService
AgentService
HighlightService
LocalAnalyticsService
```

### Providers

External or replaceable implementations end with `Provider`.

Examples:

```csharp
MockAgentProvider
OpenAIAgentProvider
```

### Tool Calls

Tool call DTOs should use explicit names.

Examples:

```csharp
AgentToolCall
AgentToolResult
HighlightComponentToolArguments
```

### Events

Events end with `Event` and describe facts that already happened.

Good:

```csharp
ProcedureStartedEvent
ProcedureStepChangedEvent
ComponentHighlightedEvent
```

Avoid command-like event names:

```csharp
StartProcedureEvent
HighlightComponentEvent
```

### ScriptableObjects

ScriptableObject assets used for authoring/configuration end with `Definition`.

Examples:

```csharp
ProcedureDefinition
EquipmentDefinition
ComponentDefinition
```

## Folder Conventions

Use this structure as the starting point:

```text
Assets/AtlasXR/Scripts/
  App/
  Core/
  Procedures/
  Agent/
  XR/
  UI/
  Analytics/
  Shared/
```

Avoid junk-drawer folders such as:

```text
Helpers/
Utils/
Misc/
Temp/
```

If a folder becomes vague, split it by responsibility.

## Dependency Rules

Keep dependencies moving inward toward stable logic.

High-level rule:

```text
UI / XR Views
  ↓
Application Services
  ↓
Domain Models
  ↓
DTOs / Data
```

Important restrictions:

- Procedure logic must not depend on UI.
- Procedure logic must not depend on XR input.
- Agent providers must not directly manipulate scene objects.
- XR visual systems must not know how AI requests are generated.
- UI should call services, not own business logic.
- Backend/remote data must be replaceable with local/mock data.

## MonoBehaviour Rules

MonoBehaviours should be thin Unity adapters.

MonoBehaviours may:

- expose serialized fields
- bind buttons and UI fields
- receive Unity callbacks
- forward input to services
- update visual state

MonoBehaviours should not:

- contain AI prompt logic
- parse AI responses
- own procedure state
- perform HTTP/API logic directly
- contain long business workflows
- act as global god objects

If a MonoBehaviour grows too large, extract logic into a plain C# service.

## Service Rules

Services own application capabilities.

Examples:

- `ProcedureService` owns current procedure state.
- `AgentService` coordinates command interpretation.
- `HighlightService` owns component highlighting requests.
- `AnalyticsService` records local analytics events.

Prefer interfaces for services that are likely to be mocked, replaced, or tested.

Do not create interfaces for tiny one-off classes unless useful.

## Agent / AI Conventions

AtlasXR uses the word **Agent** for AI-assisted behavior.

The agent should return structured tool calls, not directly control the Unity scene.

Correct flow:

```text
User command
→ AgentService
→ IAgentProvider
→ AgentToolCall
→ AgentToolExecutor
→ Application service
→ Scene/UI update
```

Incorrect flow:

```text
User command
→ AI text
→ Direct scene manipulation
```

### Initial Tools

The initial tool set should stay small:

- `HighlightComponent`
- `ClearHighlight`
- `ShowInstruction`
- `NextStep`
- `RepeatStep`
- `CompleteStep`

Add new tools only when a visible demo feature needs them.

### Tool Safety

Every tool call must be validated.

Validate:

- tool name exists
- required arguments exist
- component IDs exist
- procedure is active when needed
- step IDs are valid when needed

Invalid tool calls must fail safely.

## Procedure Conventions

Procedures should be data-driven.

A procedure should contain:

- id
- display name
- equipment id
- ordered steps
- safety warnings
- target component IDs

A step should contain:

- step id
- instruction text
- optional target component id
- optional completion rule
- optional safety note

Avoid hardcoding procedure steps in scene scripts.

## Learning Scenario Conventions

Learning scenarios connect authored procedure data to Unity scene content.

A learning scenario should contain:

- scenario id
- display name
- equipment prefab
- procedure JSON
- spawn settings

Use `LearningScenarioDefinition` assets to load equipment prefabs for demo scenarios.

Do not place scenario-specific equipment directly in the bootstrap scene when it can be loaded from a scenario definition.

The scenario loader may instantiate and destroy equipment prefabs, but it should not own procedure progression, AI decisions, highlighting behavior, or UI behavior.

Prefer direct ScriptableObject references for the MVP. Do not use `Resources.Load` for scenario content. Do not add Addressables until the project needs downloadable content, remote catalogs, or many large scenario assets.

## Async Conventions

Use `async`/`await` for IO-like operations such as:

- AI provider calls
- future backend calls
- file loading
- remote content loading

Use `CancellationToken` where meaningful.

Coroutines are acceptable for Unity timing, animations, and frame-based visual effects.

Do not mix coroutines and async code unnecessarily.

## Logging Conventions

Avoid scattered `Debug.Log` calls in production code.

Use a logging abstraction when available, such as:

```csharp
ILogger
UnityLogger
```

Logs should include useful context:

- procedure id
- step id
- component id
- tool name
- service name

Temporary debug logs are acceptable during early implementation but should be cleaned up before public portfolio presentation.

## Error Handling

Fail safely.

For MVP, errors can be shown in debug UI and logged clearly.

Examples of expected errors:

- unknown component id
- no active procedure
- invalid tool arguments
- AI provider unavailable
- procedure failed to load

Do not silently ignore errors.

## ScriptableObject Conventions

ScriptableObjects are for authoring/configuration.

They should not contain mutable runtime session state.

Good use:

- procedure definitions
- equipment definitions
- component metadata

Bad use:

- current step index
- current user state
- runtime analytics
- mutable AI conversation memory

## Data Format Conventions

The first version may use ScriptableObjects or local JSON.

Prefer DTO-style data models that can later be loaded from a backend.

Do not lock the procedure engine to Unity-only assets if avoidable.

## UI Conventions

Debug UI is allowed and encouraged during MVP.

Initial UI can include:

- typed command input
- submit button
- current instruction text
- current procedure/step display
- tool call log
- error log

Keep UI logic separate from procedure and agent logic.

## XR Conventions

Do not start with complex XR features.

Recommended order:

1. Desktop/debug mode.
2. Scene highlighting.
3. Controller ray interaction.
4. OpenXR Quest build.
5. Passthrough.
6. Hand tracking.
7. Voice.

Quest 2 is the baseline device.

Passthrough is optional in the first demo and should not be the main selling point.

## Performance Conventions

Even in a portfolio project, avoid obvious bad habits:

- Avoid unnecessary allocations in `Update`.
- Cache references.
- Avoid repeated scene searches.
- Avoid `FindObjectOfType` in production paths.
- Keep highlights and UI updates event-driven where practical.

Do not prematurely optimize before the feature works.

## Testing Conventions

Prioritize tests for pure logic:

- procedure step progression
- tool call validation
- mock agent behavior
- analytics event creation
- procedure loading/validation

Do not spend too much early time on complex Unity play mode tests.

## Comments

Comments should explain why, not what.

Bad:

```csharp
// Increment index
currentIndex++;
```

Good:

```csharp
// Advance only after tool validation so invalid AI responses cannot skip steps.
currentIndex++;
```

## Codex-Specific Conventions

When Codex modifies this project:

- Prefer small, focused changes.
- Preserve the existing folder structure.
- Preserve existing service boundaries.
- Do not introduce duplicate systems.
- Do not replace the architecture without explicit instruction.
- Do not add speculative abstractions.
- Do not add new third-party packages unless requested.
- Do not implement voice, backend, networking, or advanced XR before the basic agent tool-call loop works.
- Update relevant documentation when introducing an important architectural change.

When uncertain, implement the simplest version that supports the current milestone.
