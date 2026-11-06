---
name: ui-check
description: Audit a screen or system's visuals and UX with the ui-designer and ux-reviewer subagents against Docs/design-system.md and the jam's "teaches itself" goal.
argument-hint: <screen or system, e.g. "HUD", "combat overlay", "main menu">
disable-model-invocation: true
---

# /ui-check

Target: **$ARGUMENTS**

1. Locate the relevant UXML, USS, UI controllers and scene. If the Unity Editor is connected (Unity MCP), take a screenshot of the target at 1920×1080 and, if possible, 1280×720.
2. Run in parallel:
   - `ux-reviewer` (read-only): clarity, feedback, onboarding, readability, contrast, scaling.
   - `ui-designer`, in **audit mode only** (tell it explicitly not to edit anything this time): consistency with `Docs/design-system.md` (tokens instead of literal values, type scale, spacing, components, motion, icon + color rule).
3. Merge both reports into one list in Spanish, worst first, removing duplicates. Mark each item as visual or UX and S/M/L effort.
4. Recommend the 1-3 changes with the biggest impact on the first minute of play, and ask the developer which ones to apply. Apply only the approved ones (through `ui-designer` for visuals, `unity-developer` for logic).
