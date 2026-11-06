# GodBoard

A digital **board game of conquest** where the die is the pantheon. Made solo in one week for the **Official 2026 Unity Game Jam**. Theme: **"Lucky Number"**.

> 🚧 Work in progress: jam runs Nov 5 – Nov 14, 2026.

## The idea

Four ancient nations, four gods, one six-sided die.

| Face | Nation | God |
|---|---|---|
| ⚀ 1 | Greece | Zeus |
| ⚅ 6 | Norse | Thor |
| ⚁ 2 | Egypt | Ra |
| ⚄ 5 | Celts | Morrígan |
| ⚂ 3 | Good fortune for all | |
| ⚃ 4 | Bad omen for all | |

Opposite faces of a die always add up to 7, so **your lucky number is your rival's curse**. Every roll in the game (the Destiny Die each round, every combat, every reinforcement) is either sacred, cursed or neutral for you.

## Tech

- Unity 6 (6000.6), URP 2D, Input System
- Target: WebGL (playable in the browser)
- Game rules written as pure C#, decoupled from presentation, unit-tested, and simulated headlessly to balance the nations

## AI-assisted development

This project is built with [Claude Code](https://claude.com/claude-code) as a development partner for planning, design review, code review and balance simulations. The full disclosure of how AI was used is in [`Docs/ai-usage.md`](Docs/ai-usage.md).

## Legal

GodBoard is not sponsored by or affiliated with Unity Technologies or its affiliates. Unity is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
