# AtlasXR

**AtlasXR** is a Unity XR portfolio project focused on building an AI-powered enterprise maintenance assistant as quickly as possible, while keeping the architecture clean enough to grow later.

The goal is not to build a large framework first. The goal is to create a working, impressive vertical slice that demonstrates:

- AI integration in Unity
- Structured tool calls from an AI assistant
- Data-driven maintenance procedures
- XR-ready architecture
- Component highlighting and guided workflows
- A practical enterprise use case

## Demo Goal

The first public demo should show this flow:

```text
User opens the app
→ Sees a machine or equipment model
→ Types or says: "How do I replace the filter?"
→ AI returns a structured tool call
→ App highlights the filter
→ App shows the instruction
→ User types or says: "next step"
→ App advances the procedure
→ Next component/action is highlighted
```

This is intentionally small. It is enough to show modern XR + AI product thinking without spending months building infrastructure.

## Project Philosophy

AtlasXR follows one rule:

> Demo first. Architecture only where it directly supports the demo.

Prefer working vertical slices over large unfinished foundations.

Build reusable systems only when they are needed by the current milestone.

## Initial Scope

The first version should support a non-XR debug mode before Quest integration.

Why?

Because the AI/procedure/tool-call loop can be validated faster on desktop without fighting headset deployment, permissions, passthrough, or voice input.

Initial interaction can be typed input. Voice comes later.

## MVP Features

### Milestone 1: Desktop Debug AI Procedure Flow

- Load one sample maintenance procedure.
- Accept typed user commands.
- Use a mock AI provider first.
- Return structured tool calls.
- Execute tool calls:
  - `HighlightComponent`
  - `ShowInstruction`
  - `NextStep`
  - `RepeatStep`
- Log local analytics.
- Keep the design compatible with later XR integration.

### Milestone 2: Unity Scene Integration

- Add equipment scene objects.
- Assign component IDs to machine parts.
- Highlight components from tool calls.
- Show instructions in a simple UI.

### Milestone 3: Real AI Provider

- Add an OpenAI or other provider implementation behind an interface.
- Keep mock provider available for offline testing.
- Parse AI output into safe structured tool requests.

### Milestone 4: Quest / XR Integration

- Add OpenXR.
- Use controller ray interaction first.
- Add passthrough if available.
- Add hand tracking later.

### Milestone 5: Voice Interaction

- Add speech-to-text.
- Add text-to-speech.
- Keep typed input as a debug fallback.

## Proposed Folder Structure

```text
Assets/
  AtlasXR/
    Scripts/
      App/
        Bootstrap/
        States/
      Core/
        Events/
        Logging/
        Services/
        StateMachine/
      Procedures/
        Data/
        Runtime/
        Validation/
      Agent/
        Runtime/
        Providers/
        Tools/
        Prompts/
      XR/
        Interaction/
        Highlighting/
        Input/
        Passthrough/
      UI/
        Debug/
        Views/
      Analytics/
      Shared/
        Extensions/
        Utilities/
    ScriptableObjects/
    Prefabs/
    Scenes/
    Docs/
Docs/
  CONVENTIONS.md
  Roadmap.md
AGENTS.md
README.md
```

## Core Runtime Concept

```text
User Command
  ↓
Agent Service
  ↓
Structured Tool Call
  ↓
Tool Executor
  ↓
Unity Service
  ↓
Scene/UI/Procedure State
```

The AI must not directly modify Unity scene state. It requests actions through tools. Tools validate and execute those actions through application services.

## Example Tool Call

```json
{
  "tool": "HighlightComponent",
  "arguments": {
    "componentId": "filter_01",
    "reason": "The user asked how to replace the filter."
  }
}
```

## Suggested First Procedure

Use a simple machine with 3–5 components:

- front_panel
- filter_01
- release_latch
- replacement_filter
- power_switch

Example procedure:

```text
Replace Air Filter

1. Turn off the machine.
2. Open the front panel.
3. Release the filter latch.
4. Remove the old filter.
5. Insert the replacement filter.
6. Close the panel.
7. Confirm completion.
```

## Documentation

Start with only the documentation needed to move fast:

- `README.md` — project overview and demo goal.
- `AGENTS.md` — Codex instructions.
- `Docs/CONVENTIONS.md` — coding and architecture rules.
- `Docs/Roadmap.md` — short-term milestones.

Do not add heavier documentation until the MVP works.

## Portfolio Message

AtlasXR should communicate:

> I can build practical AI-powered XR applications with clean architecture, not just small Unity demos.

Everything in the project should support that message.
