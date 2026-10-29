---
name: orchestrate
description: Plan a feature or fix for this Unity project and delegate it to the specialist subagents (gameplay-programmer, level-builder, scenario-designer, unity-reviewer), then verify and report. Use for any multi-file or multi-system task.
argument-hint: <task description>
---

# Orchestrate: $ARGUMENTS

You are the orchestrator. You plan, delegate, integrate and verify. You do not write the implementation yourself
unless it is a trivial one-line fix.

Model routing: **scouts run on Haiku** (cheap, parallel recon), **builders on Sonnet** (`gameplay-programmer`,
`level-builder`, `scenario-designer`), **review on Opus** (`unity-reviewer`). The models are pinned in each agent's
frontmatter, so don't override them.

## 1. Understand (swarm)
- Read `CLAUDE.md`, then split what you need to know into 3–6 narrow questions (e.g. "which events does GameManager expose?", "how does BuildNPC wire an NPC?", "which spot ids exist?").
- Launch one `scout` per question **in a single message so they run in parallel**. Read their file:line evidence; open a file yourself only when a scout was unsure.
- If the request is ambiguous in a way that changes the design, ask the user one focused question. Otherwise decide and note the assumption.

## 2. Plan
Break the task into work packages. For each, state:
- **Owner** — one of `gameplay-programmer`, `level-builder`, `scenario-designer`.
- **Files** it may touch (explicit list or folder).
- **Contract** — public API, events, serialized fields, string ids it must create or consume, so packages fit together.
- **Done when** — concrete, checkable outcome.

Show the plan to the user in a short table before delegating.

## 3. Delegate
- Brief each agent with: the goal, its package, the contract, files it owns, files it must NOT touch, and done-when. Agents start cold — include everything they need.
- **Parallel only when file sets are disjoint.** There is no git here, so two agents editing the same file will clobber each other. Typical order when dependent: `gameplay-programmer` (new types/fields) → `scenario-designer` (data using them) → `level-builder` (scene wiring).
- If an agent reports a blocker or a contract change, update the plan and re-brief the affected agents.

## 4. Verify
- Run `unity-reviewer` on the full set of changed files with the contract and done-when list.
- Fix loop: send each finding back to the owning agent, then re-review. Stop after 2 rounds and report what's left.
- If generated content changed, remind the user which Tools > Prevent the Murder menu item to re-run (or run it through the Unity CLI if an editor is connected).

## 5. Report
Short summary: what changed (files), what was verified and how, what still needs a human (play-testing, menu re-runs, open questions).
