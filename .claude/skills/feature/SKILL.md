---
name: feature
description: Run the full feature workflow with the project's subagents (unity-developer → code-reviewer → ui-designer + ux-reviewer when UI is touched → fixes → commit proposal).
argument-hint: <feature description>
disable-model-invocation: true
---

# /feature

Feature to build: **$ARGUMENTS**

Run this workflow. Talk to the developer in Spanish; code and commits stay in English.

1. **Frame it (main session).** Read `CLAUDE.md`. Restate the feature in 2-3 lines, check it against the design filter ("if the lucky number were removed, would this still work?") and the MVP list. If it is a stretch goal and the MVP is not done, or it is past feature freeze, say so and ask before continuing. Make sure the current branch is a `feature/...` branch (propose a name if on `main`).
2. **Implement.** Delegate to the `unity-developer` subagent with a precise brief: goal, acceptance criteria, files/areas, tests expected.
3. **Review.** Delegate to `code-reviewer` on the resulting diff. If the verdict is not `Ready to commit`, send the HIGH/MEDIUM findings back to `unity-developer` and repeat review once.
4. **UI/UX (only if screens, HUD, feedback or feel changed).** Run `ui-designer` to align visuals with `Docs/design-system.md`, then `ux-reviewer` (they can run one after the other; the reviewer is read-only). Apply BLOCKER/MAJOR fixes through `ui-designer` or `unity-developer`.
5. **Verify.** Tests pass, the console has no errors (Unity MCP `read_console` if available).
6. **Wrap up.** Summarize for the developer in Spanish: what was built, the key design decision (so they can explain it), test results, open issues, and anything they must do in the Editor. Propose a commit message (imperative, English, **no Co-Authored-By or AI attribution**) and commit only after the developer agrees. If AI did significant work, add a line to `Docs/ai-usage.md`.
