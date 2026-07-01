# AtlasXR Roadmap

## Roadmap Principle

AtlasXR is being built under time pressure as a portfolio project.

The roadmap prioritizes a visible AI-powered demo over a complete framework.

Every milestone should produce something that can be shown in a short video, screenshot, or GitHub update.

## Current Target

Build a working AI-powered maintenance assistant vertical slice:

```text
Typed command
→ Agent response
→ Structured tool call
→ Component highlight
→ Instruction display
→ Step progression
```

Voice, backend, and advanced XR come later.

## Milestone 0 — Project Setup

Goal: create a clean starting point for Codex-assisted development.

### Tasks

- Create Unity project.
- Add root `README.md`.
- Add root `AGENTS.md`.
- Add `Docs/CONVENTIONS.md`.
- Add `Docs/Roadmap.md`.
- Create base folder structure under `Assets/AtlasXR`.
- Create initial scene.

### Done When

- Project opens successfully.
- Documentation is committed.
- Folder structure exists.
- Codex can read and follow the project docs.

## Milestone 1 — Non-XR Agent Procedure Loop

Goal: prove the AI/procedure/tool architecture without Quest or XR complexity.

### Tasks

- Create procedure domain models.
- Create one sample procedure: `replace_air_filter`.
- Create procedure service.
- Create mock agent provider.
- Create agent service.
- Create structured tool call DTOs.
- Create tool executor.
- Create debug UI with typed input.
- Show current instruction.
- Log executed tool calls.

### Initial Tools

- `HighlightComponent`
- `ShowInstruction`
- `NextStep`
- `RepeatStep`

### Done When

A user can type:

```text
How do I replace the filter?
```

And the app responds by:

- selecting the correct procedure context
- producing a tool call
- showing the correct instruction
- logging the tool call

Then the user can type:

```text
next
```

And the procedure advances.

## Milestone 2 — Scene Component Highlighting

Goal: connect tool calls to visible Unity scene behavior.

### Tasks

- Create simple equipment model from primitives or free placeholder assets.
- Add component identifiers to relevant objects.
- Create `EquipmentComponent` MonoBehaviour.
- Create highlight service.
- Create simple highlight visual effect.
- Connect `HighlightComponent` tool to scene object highlighting.
- Add `ClearHighlight` tool.

### Done When

A typed command causes the correct machine component to glow or otherwise visibly highlight.

A 30-second video can show:

```text
Typed user command → AI/tool response → filter highlights in scene.
```

This is the first meaningful portfolio checkpoint.

## Milestone 3 — Real AI Provider

Goal: replace mock-only behavior with a real AI provider while keeping the mock provider for testing.

### Tasks

- Add `IAgentProvider` abstraction if not already present.
- Keep `MockAgentProvider`.
- Add real provider implementation.
- Add safe API key handling.
- Add prompt template for maintenance assistant behavior.
- Request structured tool-call output.
- Validate AI responses before execution.
- Add fallback behavior if provider fails.

### Done When

The app can send typed user commands to a real AI provider and receive valid structured tool calls.

Mock mode still works without internet/API access.

## Milestone 4 — Portfolio Polish Pass 1

Goal: make the current demo understandable to recruiters and senior engineers.

### Tasks

- Improve README with screenshots/GIF/video link placeholders.
- Add short architecture explanation.
- Add sample procedure format.
- Add list of supported tools.
- Clean temporary debug logs.
- Add comments only where they explain important reasoning.
- Ensure scene can be run easily from a clean checkout.

### Done When

A reviewer can understand the project in under five minutes by reading the README and running the demo scene.

## Milestone 5 — Quest / OpenXR Integration

Goal: make the demo run on Quest with basic XR interaction.

### Tasks

- Add OpenXR setup.
- Configure Quest build settings.
- Add controller ray interaction.
- Make debug command input accessible or replace it with simple in-headset UI.
- Ensure highlighting works on Quest.
- Test on Quest 2.

### Done When

The same maintenance procedure flow works in headset using basic XR interaction.

Hand tracking is not required yet.

Passthrough is optional.

## Milestone 6 — Voice Interaction

Goal: make the demo feel like a modern AI assistant.

### Tasks

- Add speech-to-text service interface.
- Add mock speech input for editor/debug mode.
- Add real speech-to-text provider.
- Add text-to-speech service interface.
- Add simple spoken instruction output.
- Keep typed input as fallback.

### Done When

User can speak a command and hear or see the assistant response.

Typed input still works.

## Milestone 7 — Mixed Reality Enhancements

Goal: make the demo feel more relevant to enterprise XR.

### Tasks

- Add passthrough mode where supported.
- Add simple world-space UI.
- Add optional hand tracking.
- Improve spatial placement of instruction panels and arrows.

### Done When

The demo communicates a mixed-reality maintenance assistant concept, even if Quest 2 passthrough quality is limited.

## Milestone 8 — Analytics and Session Summary

Goal: show product thinking beyond the demo interaction.

### Tasks

- Track procedure started.
- Track step completed.
- Track repeated instruction.
- Track invalid command/tool failure.
- Show local completion summary.
- Export or log analytics locally.

### Done When

The app shows a simple session summary after completing the procedure.

## Not Yet

Avoid these until the core demo works:

- Full backend.
- User accounts.
- Multiplayer/collaboration.
- Complex CAD import.
- Large procedural framework.
- Multiple AI providers.
- Advanced hand tracking gestures.
- Full enterprise admin dashboard.

These may be valuable later, but they are not needed for the first portfolio demo.

## Recommended Weekly Focus

### Week 1

Milestone 0 + Milestone 1.

Output: working typed debug procedure loop.

### Week 2

Milestone 2.

Output: visible scene highlighting from tool calls.

### Week 3

Milestone 3.

Output: real AI provider controlling safe tool calls.

### Week 4

Milestone 4 + start Milestone 5.

Output: portfolio-ready README and first Quest build attempt.

## Portfolio Checkpoints

Create short videos at these points:

1. Mock agent tool-call loop.
2. AI highlights component in Unity scene.
3. Real AI provider controls the procedure.
4. Quest build with XR interaction.
5. Voice-driven assistant flow.

Each video should be 30–90 seconds.

The goal is to show progress publicly without waiting for the entire project to be complete.
