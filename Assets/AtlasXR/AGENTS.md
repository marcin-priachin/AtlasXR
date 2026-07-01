# AGENTS.md

## Purpose

This file gives instructions for AI coding agents working on **AtlasXR**.

AtlasXR is a Unity XR portfolio project. The immediate priority is to build a working AI-powered maintenance assistant demo quickly.

Do not over-engineer the foundation before the demo works.

## Required Reading

Before modifying code, read:

1. `README.md`
2. `Docs/CONVENTIONS.md`
3. `Docs/Roadmap.md`

Follow those files strictly.

If a request conflicts with the conventions or roadmap, mention the conflict before making changes.

## Primary Goal

The short-term goal is a working vertical slice:

```text
Typed user command
→ Agent interprets request
→ Agent returns structured tool call
→ Tool executor validates request
→ Procedure/Highlight/UI services execute it
→ User sees highlighted component and instruction
```

Typed input comes before voice.

Desktop/debug mode comes before Quest deployment.

Mock AI comes before real AI provider.

## Development Rule

Use this rule for every change:

> Demo first. Architecture only where it directly supports the demo.

Do not introduce abstractions unless they are needed by the current or next milestone.

## Current MVP Priorities

Implement in this order:

1. Procedure model and sample procedure.
2. Mock agent provider.
3. Structured tool call model.
4. Tool executor.
5. Debug input UI.
6. Instruction display.
7. Component highlighting.
8. Local analytics logging.
9. Real AI provider.
10. XR input and Quest integration.
11. Voice input/output.

## Architecture Rules

Use services for major capabilities:

- `IProcedureService`
- `IAgentService`
- `IAgentProvider`
- `IAgentToolExecutor`
- `IHighlightService`
- `IInstructionDisplayService`
- `IAnalyticsService`

Prefer small interfaces and simple implementations.

Do not create large generic frameworks unless specifically requested.

## Agent / AI Rules

The AI must never directly modify scene state.

The agent may only request structured tool calls.

Allowed initial tools:

- `HighlightComponent`
- `ClearHighlight`
- `ShowInstruction`
- `NextStep`
- `RepeatStep`
- `CompleteStep`

All tool calls must be validated before execution.

Invalid tool calls should fail safely and produce a useful log message.

Keep a mock provider available even after adding a real provider.

## Unity Rules

MonoBehaviours should be thin.

MonoBehaviours may:

- hold serialized references
- receive Unity lifecycle callbacks
- bind UI controls
- trigger service calls
- update visuals

MonoBehaviours should not contain business logic, procedure logic, AI logic, or networking logic.

## Codex Behavior Rules

When generating code:

- Preserve existing architecture.
- Prefer extending existing systems over creating duplicates.
- Do not rename public APIs unless requested.
- Do not replace the event/state/service architecture without explicit instruction.
- Do not add third-party packages unless requested.
- Do not introduce backend, voice, networking, or complex XR features before the basic AI tool-call flow works.
- Keep changes small and reviewable.
- Update documentation when introducing a new important convention or system.

## When Unsure

Prefer the simplest implementation that supports the current milestone.

Avoid speculative future-proofing.

A working demo with clean seams is better than an unfinished framework.

## First Task Recommendation

If starting from an empty project, begin with:

1. Create folder structure.
2. Create procedure data model.
3. Create one sample procedure.
4. Create mock agent provider.
5. Create tool call DTOs.
6. Create tool executor.
7. Create debug UI for typed commands.
8. Show current instruction in UI.
9. Log tool execution.

Do not start with OpenXR, passthrough, voice, backend, or real API calls.
