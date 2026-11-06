---
name: scope
description: Ask the jam-producer subagent where the project stands against the calendar and rubric, what to cut, and the next three tasks.
argument-hint: "[optional: question, e.g. 'can we still add god powers?']"
disable-model-invocation: true
---

# /scope

Question (optional): **$ARGUMENTS**

1. Delegate to the `jam-producer` subagent. Give it today's date, the current branch, and the developer's question if any.
2. Relay its report to the developer in Spanish, keeping its structure (status, done, remaining MVP, cut/defer, next 3 tasks, main risk).
3. If it recommends cutting something, ask the developer to confirm before updating the MVP list in `CLAUDE.md`.
