---
name: review-code
description: Review the current uncommitted and branch changes with the read-only code-reviewer subagent and report prioritized findings.
argument-hint: "[optional: file, folder or focus area]"
disable-model-invocation: true
---

# /review-code

Focus (optional): **$ARGUMENTS**

1. Collect the scope: `git status`, `git diff`, `git diff --staged`, and the commits on this branch not in `main` (`git log main..HEAD --oneline`, `git diff main...HEAD`). If a focus was given, narrow to it.
2. Delegate to the `code-reviewer` subagent with that scope. It is read-only and returns a verdict plus prioritized findings.
3. Present the result to the developer in Spanish: the verdict, then the findings grouped HIGH → LOW with `file:line` links. Do **not** change any code.
4. End by asking whether to fix the HIGH/MEDIUM findings (via `unity-developer`) or leave them.
