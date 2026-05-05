# Claude Code Development System for C#/WPF/Revit Add-in Projects

**Date:** 2026-03-21
**Scope:** Global setup across all C#/WPF/Revit Add-in projects
**Target workflow:** Investigate -> Plan -> Implement -> Build -> Review -> (loop back if issues) -> Test in Revit -> Commit -> PR

---

## 1. Overview

A layered automation system for Claude Code, tailored to C# WPF Revit Add-in development. Three layers work together, each doing what it does best:

- **Hooks** — hard gates that enforce rules automatically (build must pass, commits must be clean)
- **Skills** — phase-specific guidance invoked via slash commands (scaffolding, review, research)
- **Agents** — specialized subagents that Claude dispatches when contextually relevant

The system is framework-aware (supports .NET Framework 4.8 for Revit 2024 and .NET 8.0-windows for Revit 2026), detected per-project from `.csproj` files.

### How agents work in Claude Code

Agents are `.md` files in `~/.claude/agents/`. Claude reads their `description` field and decides when to dispatch them based on context. Skills **cannot programmatically dispatch agents** — they provide instructions that guide Claude to consider using relevant agents. Claude makes the final dispatch decision.

---

## 2. Hooks

All hooks are defined in `~/.claude/settings.json` using the Claude Code hooks schema. Each hook entry specifies an event type, an optional matcher, and one or more hook actions.

### Hook schema reference

```jsonc
{
  "hooks": {
    "<EventType>": [
      {
        "matcher": "<optional regex or * to filter>",
        "hooks": [
          {
            "type": "command",           // "command" runs shell, "prompt" injects text
            "command": "shell command",
            "timeout": 10                // seconds
          }
        ]
      }
    ]
  }
}
```

**Event types used in this design:** `SessionStart`, `PreToolUse`, `PostToolUse`

**Exit codes:** `0` = success/approve, `2` = deny/block, other = continue

**Note:** There is no `PreCommit` or `PostCommit` event in Claude Code. Commit-related enforcement uses `PreToolUse` with matcher `Bash` and inspects `$TOOL_INPUT` for git commit commands.

### 2.1 Build Gate (Pre-Commit)

- **Event:** `PreToolUse` with matcher `Bash`
- **Action:** When the Bash tool input contains `git commit`, runs `dotnet build` first
- **On failure (exit 2):** Blocks the commit
- **Windows-safe:** Uses `grep` (available in Git Bash on this system)

```jsonc
{
  "matcher": "Bash",
  "hooks": [
    {
      "type": "command",
      "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && { PROJECT=$(grep -rl \"<TargetFramework\" --include=\"*.csproj\" . 2>/dev/null | head -1); if [ -n \"$PROJECT\" ]; then dotnet build \"$PROJECT\" --no-restore 2>&1 | tail -20 || exit 2; fi; } || exit 0'",
      "timeout": 60
    }
  ]
}
```

### 2.2 Framework Detector (SessionStart)

- **Event:** `SessionStart`
- **Action:** Reads `.csproj` `<TargetFramework>` elements and outputs them
- **Purpose:** Claude sees the framework in context and adjusts API suggestions accordingly

```jsonc
{
  "matcher": "*",
  "hooks": [
    {
      "type": "command",
      "command": "bash -c 'grep -rh \"<TargetFramework\" --include=\"*.csproj\" . 2>/dev/null | head -5 || echo \"No .csproj found\"'"
    }
  ]
}
```

### 2.3 Commit Message Lint (Post-Commit)

- **Event:** `PostToolUse` with matcher `Bash`
- **Action:** After a Bash command containing `git commit`, validates the commit message
- **Non-blocking:** Outputs a warning only (exit 0)

```jsonc
{
  "matcher": "Bash",
  "hooks": [
    {
      "type": "command",
      "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && { MSG=$(git log -1 --pretty=%s 2>/dev/null); echo \"$MSG\" | grep -qE \"^(feat|fix|refactor|chore|docs|style|test|build|perf)(\\\\(.+\\\\))?:\" || echo \"WARNING: Commit message does not follow conventional format. Expected: type(scope): description\"; } || exit 0'",
      "timeout": 5
    }
  ]
}
```

### 2.4 Diff Size Warning (Pre-Commit)

- **Event:** `PreToolUse` with matcher `Bash`
- **Action:** When `git commit` is detected, checks staged diff size. Warns if >500 lines.
- **Non-blocking:** Warning only (exit 0)

```jsonc
{
  "matcher": "Bash",
  "hooks": [
    {
      "type": "command",
      "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && { LINES=$(git diff --cached --stat 2>/dev/null | tail -1 | grep -oE \"[0-9]+ insertion\" | grep -oE \"[0-9]+\"); [ \"${LINES:-0}\" -gt 500 ] && echo \"WARNING: Staged diff is ${LINES} lines. Consider splitting into smaller commits.\"; } || exit 0'",
      "timeout": 5
    }
  ]
}
```

### 2.5 Escape Hatch

To temporarily disable hooks without editing `settings.json`, rename the hooks key:

```jsonc
// In settings.json, rename "hooks" to "_hooks" to disable all hooks
// Or remove individual hook entries
```

---

## 3. Skills (Slash Commands)

Skills are stored in `~/.claude/skills/<name>/SKILL.md` — each skill is a **directory** containing a `SKILL.md` file and optional reference materials.

### Skill file format

```yaml
---
name: skill-name
description: When to use this skill — triggers Claude's auto-invocation
disable-model-invocation: true   # Only user can invoke via /skill-name
allowed-tools: Read, Grep, Glob  # Optional tool restrictions
---

Skill instructions in markdown. Use $ARGUMENTS for user-provided args.
Use !`command` for dynamic context injection.
```

### 3.1 `/wpf-new` — WPF Scaffold

**Location:** `~/.claude/skills/wpf-new/SKILL.md`

```yaml
---
name: wpf-new
description: Generate WPF boilerplate. Use when asked to scaffold a new view, command, converter, behavior, or dialog.
disable-model-invocation: true
---
```

- **Phase:** Implement
- **Purpose:** Generates boilerplate for common WPF/MVVM patterns
- **Arguments:** `view <Name>`, `command <Name>`, `converter <Name>`, `behavior <Name>`, `dialog <Name>`
- **Dynamic context:** Reads target framework from `.csproj` via `!`grep -rh "<TargetFramework" --include="*.csproj" . 2>/dev/null | head -1``
- **Conventions enforced:**
  - ViewModels in `ViewModels/` folder, Views in `Views/` folder
  - Nullable reference types enabled
  - No code-behind logic — all in ViewModel
  - C# 12 syntax (primary constructors, collection expressions where appropriate)
- **Reference files:** `templates/` directory with C# templates for each pattern

### 3.2 `/revit-new` — Revit Command Scaffold

**Location:** `~/.claude/skills/revit-new/SKILL.md`

- **Phase:** Implement
- **Purpose:** Generates boilerplate for Revit API patterns
- **Arguments:** `command <Name>`, `event <Name>`, `updater <Name>`, `panel <Name>`, `application <Name>`
- **Framework-aware:** Detects target framework and adjusts patterns:
  - .NET 4.8: `TaskDialog`, classic Transaction patterns
  - .NET 8.0: async patterns where supported, nullable annotations
- **Conventions enforced:**
  - Transaction wrapped in `using` with explicit `Start()`/`Commit()`
  - Error handling with `TaskDialog` for user-facing errors
  - Follows Revit API best practices (no modeless operations in command context, etc.)
- **Reference files:** `templates/` with Revit boilerplate for each pattern

### 3.3 `/solid-review` — SOLID/Clean Code Review

**Location:** `~/.claude/skills/solid-review/SKILL.md`

- **Phase:** Pre-commit review
- **Runs in:** Forked sonnet subagent (`context: fork`) — saves opus tokens
- **Purpose:** Reviews changed files against Clean Code and SOLID principles
- **Dynamic context:**
  - `!`git diff --cached --name-only`` — staged files
  - `!`git diff --name-only`` — unstaged changes (fallback)
- **Instructions to Claude:**
  1. Identify changed `.cs` and `.xaml` files
  2. Read each changed file
  3. For `.cs` files: analyze for SOLID violations (SRP, OCP, LSP, ISP, DIP), Clean Code issues (magic numbers, deep nesting, long methods)
  4. For `.xaml`/ViewModel files: check MVVM compliance, binding correctness, DependencyProperty conventions
  5. For files using Revit API: check Transaction lifecycle, element access context, deprecated API usage
  6. Report findings with severity (Error/Warning/Info) and file:line references
- **Relationship to agents:** The `/solid-review` skill provides the workflow and instructions. Claude dispatches `solid-checker` to perform the actual SOLID analysis. `code-improvement-advisor` covers broader readability/performance suggestions — complementary, not overlapping.

### 3.4 `/build` — Removed

~~Originally planned as a separate skill.~~ **Removed** — Claude already runs `dotnet build` and parses errors naturally. Adding a skill for this just loads extra tokens for something Claude does inline. The PreToolUse hook (2.1) handles the commit gate. If a build fails during implementation, Claude diagnoses it directly.

---

## 4. Agents (Subagents)

Agents are defined in `~/.claude/agents/<name>.md`. Claude reads the `description` (including `<example>` blocks) and decides when to dispatch them.

### Agent file format

```yaml
---
name: agent-name
description: "Use this agent when [condition].\n\n<example>\nContext: [situation]\nuser: \"[what user says]\"\nassistant: \"[how Claude responds]\"\n<commentary>\n[why this triggers the agent]\n</commentary>\n</example>"
model: sonnet
color: green
---

System prompt for the agent in markdown.
```

**Fields:** `name` (required), `description` (required, with examples), `model` (`sonnet`/`opus`/`haiku`), `color` (`blue`/`cyan`/`green`/`yellow`/`magenta`/`red`), `tools` (optional array), `memory` (optional)

### 4.1 `codebase-investigator`

- **Model:** haiku (read-only exploration — pattern-matching, no code writing)
- **Color:** magenta
- **Triggered when:** Starting a new task, investigating a bug, or exploring how something works in the codebase
- **Primary caller:** `/investigate` skill dispatches this agent in Step 2 alongside a git/web research agent (parallel)
- **Responsibilities:**
  - Searches the codebase for existing solutions, similar patterns, or related implementations
  - Maps dependencies — what classes/interfaces are involved, how they connect
  - Identifies reusable code that could solve or partially solve the task
  - Finds where similar problems were solved before (naming patterns, architectural patterns)
  - Reports findings: relevant files with paths, existing patterns to follow, potential reuse opportunities
- **Tools:** Read, Grep, Glob (read-only — never writes)
- **Output contract:** Returns a structured report that `/investigate` merges with git/web research in Step 3:
  - **Relevant files:** paths + brief description of each
  - **Existing patterns:** how similar problems are currently solved
  - **Reuse opportunities:** specific classes/methods that can be extended or composed
  - **Dependency map:** key types involved and their relationships

### 4.2 `revit-api-expert`

- **Model:** sonnet (dual role — needs reasoning for both implementation and review)
- **Color:** yellow
- **Triggered when:** Code touches Revit API types (Document, Element, Transaction, FilteredElementCollector, etc.)
- **Dual role:**
  - **As implementer** (in Revit pipeline): writes Revit API code — commands, events, updaters, element operations
  - **As reviewer** (in review step): checks existing Revit API code for pitfalls
  - Stays on sonnet for both roles because Revit API patterns require deeper reasoning than pattern-matching
- **Responsibilities:**
  - Reviews Transaction lifecycle (Start/Commit/Dispose, nested transactions)
  - Checks for common pitfalls: accessing elements outside valid context, modifying read-only parameters, wrong BuiltInParameter usage
  - Validates event handler context (modeless vs modal, ExternalEvent usage)
  - Flags deprecated Revit API calls per target version
  - Framework-aware: different patterns for 4.8 vs 8.0
- **Tools:** Read, Grep, Glob, WebSearch, WebFetch

### 4.3 `wpf-reviewer`

- **Model:** haiku (review role — pattern-matching, lower token cost)
- **Color:** cyan
- **Triggered when:** `.xaml` or ViewModel files are modified
- **Responsibilities:**
  - Checks for MVVM violations (logic in code-behind, event handlers instead of commands)
  - Validates data binding patterns (missing INotifyPropertyChanged, incorrect binding paths)
  - Reviews visual tree structure for performance issues
  - Checks resource dictionary usage and style consistency
  - Validates DependencyProperty declarations (correct metadata, callbacks)
- **Tools:** Read, Grep, Glob

### 4.4 `solid-checker`

- **Model:** haiku (review role — pattern-matching, lower token cost)
- **Color:** green
- **Triggered when:** During code review, `/solid-review`, or when Claude detects SOLID-relevant changes
- **Responsibilities:**
  - Analyzes `.cs` files for SOLID violations:
    - **SRP:** Class/method doing more than one thing, method >20 lines, class >200 lines
    - **OCP:** Switch statements on type, hardcoded dependencies
    - **LSP:** Derived types that throw NotImplementedException
    - **ISP:** Interfaces with >5 methods, unused interface members
    - **DIP:** Direct `new` of dependencies (vs injection), static service locator usage
  - Clean Code checks: magic numbers, unclear names, deep nesting (>3 levels)
  - Returns findings with severity, file:line, and suggested fix
- **Tools:** Read, Grep, Glob

**Note on `build-validator` removal:** The original design had a separate `build-validator` agent. This has been removed because the PreToolUse build hook (2.1) already gates commits, and the `/build` skill (3.4) handles intelligent build diagnostics. A third build component would be redundant.

---

## 5. File Structure

```
~/.claude/
├── CLAUDE.md                                    # Global instructions (already exists)
├── settings.json                                # Hooks + plugins (update existing)
├── agents/
│   ├── senior-wpf-developer.md                  # Already exists
│   ├── code-improvement-advisor.md              # Already exists
│   ├── codebase-investigator.md                  # NEW
│   ├── revit-api-expert.md                      # NEW
│   ├── wpf-reviewer.md                          # NEW
│   └── solid-checker.md                         # NEW
└── skills/
    ├── wpf-new/
    │   ├── SKILL.md                             # NEW
    │   └── templates/
    │       ├── view.cs.template
    │       ├── viewmodel.cs.template
    │       ├── command.cs.template
    │       ├── converter.cs.template
    │       ├── behavior.cs.template
    │       └── dialog.cs.template
    ├── revit-new/
    │   ├── SKILL.md                             # NEW
    │   └── templates/
    │       ├── external-command.cs.template
    │       ├── external-event.cs.template
    │       ├── updater.cs.template
    │       ├── dockable-panel.cs.template
    │       └── external-application.cs.template
    └── solid-review/
        └── SKILL.md                             # NEW
```

---

## 6. Merged settings.json

The existing `settings.json` will be updated to add hooks while preserving current config:

```jsonc
{
  // Existing config preserved
  "enabledPlugins": {
    "superpowers@claude-plugins-official": true
  },
  "extraKnownMarketplaces": {
    "claude-plugins-official": {
      "source": {
        "source": "github",
        "repo": "anthropics/claude-plugins-official"
      }
    }
  },
  "autoUpdatesChannel": "latest",
  "model": "opus[1m]",

  // NEW: hooks
  "hooks": {
    "SessionStart": [
      {
        "matcher": "*",
        "hooks": [
          {
            "type": "command",
            "command": "bash -c 'grep -rh \"<TargetFramework\" --include=\"*.csproj\" . 2>/dev/null | head -5 || echo \"No .csproj found\"'"
          }
        ]
      }
    ],
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && { PROJECT=$(grep -rl \"<TargetFramework\" --include=\"*.csproj\" . 2>/dev/null | head -1); if [ -n \"$PROJECT\" ]; then dotnet build \"$PROJECT\" --no-restore 2>&1 | tail -20 || exit 2; fi; } || exit 0'",
            "timeout": 60
          },
          {
            "type": "command",
            "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && { LINES=$(git diff --cached --stat 2>/dev/null | tail -1 | grep -oE \"[0-9]+ insertion\" | grep -oE \"[0-9]+\"); [ \"${LINES:-0}\" -gt 500 ] && echo \"WARNING: Staged diff is ${LINES} lines. Consider splitting into smaller commits.\"; } || exit 0'",
            "timeout": 5
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && { MSG=$(git log -1 --pretty=%s 2>/dev/null); echo \"$MSG\" | grep -qE \"^(feat|fix|refactor|chore|docs|style|test|build|perf)(\\\\(.+\\\\))?:\" || echo \"WARNING: Commit message does not follow conventional format. Expected: type(scope): description\"; } || exit 0'",
            "timeout": 5
          }
        ]
      }
    ]
  }
}
```

---

## 7. What Already Exists (No Work Needed)

| Component | Type | Phase | Source |
|---|---|---|---|
| `investigating-problem` | Skill | Investigate | User skill |
| `revit-api-research` | Skill | Investigate | User skill |
| `superpowers:brainstorming` | Skill | Plan | Superpowers plugin |
| `superpowers:writing-plans` | Skill | Plan | Superpowers plugin |
| `superpowers:test-driven-development` | Skill | Implement | Superpowers plugin |
| `superpowers:systematic-debugging` | Skill | Debug | Superpowers plugin |
| `senior-wpf-developer` | Agent | Implement | User agent |
| `code-improvement-advisor` | Agent | Review | User agent |
| `superpowers:requesting-code-review` | Skill | Review | Superpowers plugin |
| `commit` | Skill | Commit | User skill |
| `create-pr` | Skill | PR | User skill |
| `superpowers:verification-before-completion` | Skill | Pre-commit | Superpowers plugin |

---

## 8. Development Flow

```
Investigate ──> Plan ──> Agent Pipelines (parallel or sequential) ──> Test in Revit ──> Commit ──> PR
    |              |                                                                      |        |
    v              v                                                                      v        v
/investigate  brainstorm      ┌─────────────────────────────┐                         /commit /create-pr
/revit-       writing-plans   │  Pipeline per task:          │                          build
 research     (decides        │                              │                          hook
codebase-     pipelines)      │  Implement ──> Build ──> Review                        commit lint
 investigator                  │      ^                   |   │                         diff warn
                              │      +── fix loop ──────-+   │
                              │                              │
                              │  WPF:   senior-wpf-dev       │
                              │         /build + wpf-reviewer │
                              │                              │
                              │  Revit: revit-api-expert     │
                              │         /build + solid-checker│
                              │                              │
                              │  C#:    Claude direct        │
                              │         /build + code-improv. │
                              └─────────────────────────────┘
```

### Implementation model: plan-driven agent teams

The **Plan phase** determines whether tasks are independent enough for parallel agent teams or must be sequential. Each implementation agent runs in its own **pipeline** with a paired build and review agent.

#### Agent pipelines

Each pipeline is a self-contained loop:

```
[Implement Agent] ──> [Build Agent] ──> [Review Agent]
        ^                                     |
        +─── fix issues ─────────────────────-+
```

| Pipeline | Implement | Build | Review |
|---|---|---|---|
| **WPF pipeline** | `senior-wpf-developer` | `/build` | `wpf-reviewer` + `solid-checker` |
| **Revit API pipeline** | `revit-api-expert` | `/build` | `solid-checker` |
| **General C# pipeline** | Claude (direct) | `/build` | `solid-checker` + `code-improvement-advisor` |

#### When to use parallel vs sequential pipelines

Decided during the **Plan phase** (`superpowers:writing-plans`):

- **Parallel pipelines** — when the plan identifies independent tasks that don't share files or interfaces (e.g., a new converter + an unrelated Revit command)
- **Sequential pipeline** — when tasks are tightly coupled (e.g., ViewModel depends on Revit API service class). One pipeline finishes before the next starts.
- **Single pipeline** — when the task is small or everything is coupled

#### Pipeline flow

1. **Plan** identifies tasks and assigns pipelines (parallel or sequential)
2. Each pipeline's **implement agent** writes code (in a worktree if parallel)
3. Each pipeline's **build agent** verifies compilation
4. Each pipeline's **review agent(s)** check the result
5. If review finds issues → loop back to implement within that pipeline
6. Once all pipelines pass → **Proceed** to Test in Revit -> Commit -> PR
   (Build hook at commit time catches any cross-pipeline conflicts)

#### No separate cross-pipeline review needed

Each pipeline already includes build + review. If parallel pipelines produce conflicting code, the PreToolUse build hook catches it at commit time (won't compile = blocked). This avoids a redundant review step.

### Investigate -> Plan handoff

`/investigate` outputs feed directly into brainstorming. Brainstorming **skips context gathering** (already done) and focuses only on design decisions:

1. `/investigate` delivers: root cause, relevant files, existing patterns, proposed solutions
2. `superpowers:brainstorming` receives those findings and focuses on:
   - **Design decisions** that investigation didn't resolve (architecture choices, trade-offs)
   - **Approach selection** — which of the proposed solutions to pursue
   - **Pipeline assignment** — which agents handle which tasks
   - Skips: codebase exploration, context gathering, problem analysis (already done)
3. `superpowers:writing-plans` takes the approved design and creates the implementation plan, including:
   - **Pipeline budget** — how many pipelines to run, balancing token cost vs work volume
   - **Task grouping** — small related tasks grouped into one pipeline to avoid overhead
   - **Pipeline assignments** — which agents handle which task groups

### Pipeline budgeting (decided during Plan phase)

The plan should explicitly weigh cost vs parallelism:

| Work size | Pipeline strategy | Rationale |
|---|---|---|
| Small (1-2 files, one domain) | Single pipeline, no agents | Agent overhead costs more than doing it directly |
| Medium (3-5 files, one domain) | Single pipeline, one implement agent | Agent adds value, but parallelism not needed |
| Medium (3-5 files, mixed domains) | 2 pipelines if domains are independent | WPF and Revit can run in parallel |
| Large (6+ files, multiple domains) | 2-3 pipelines max | More pipelines = more token overhead, diminishing returns |

**Rules:**
- Never more than 3 parallel pipelines — token cost scales linearly, value doesn't
- Group small tasks into one pipeline rather than giving each its own
- Skip agent dispatch for trivial changes (rename, one-line fix) — do them directly
- Scaffolding (`/wpf-new`, `/revit-new`) doesn't need a pipeline — just run the skill

### How the layers interact

1. **SessionStart hook** detects framework -> visible to Claude for all subsequent work
2. **User invokes skill** (e.g., `/solid-review`) -> skill instructions guide Claude to read changed files and consider dispatching relevant agents
3. **Claude dispatches agents** based on file types and content (Revit API types -> `revit-api-expert`, XAML -> `wpf-reviewer`, C# -> `solid-checker`)
4. **If review finds issues** -> loop back to Implement -> Build -> Review
5. **PreToolUse hook on Bash** -> when `git commit` is detected, build gate runs and diff size is checked
6. **PostToolUse hook on Bash** -> after `git commit` succeeds, commit message format is validated

---

## 9. Token Cost Strategy

| Component | Model | Cost | Rationale |
|---|---|---|---|
| Hooks | N/A (shell) | Free | No LLM tokens |
| `/wpf-new`, `/revit-new` skills | Inherits parent | Low | Loaded once per invocation |
| `/solid-review` skill | sonnet (forked) | Low | Runs in subagent, not opus |
| `/investigate` skill | sonnet (forked) | Low | Runs in subagent, not opus |
| `codebase-investigator` | haiku | Low | Read-only exploration, pattern-matching |
| `senior-wpf-developer` | sonnet | Medium | Implementation needs reasoning |
| `revit-api-expert` | sonnet | Medium | Implementation needs API knowledge |
| `wpf-reviewer` | haiku | Low | Pattern-matching review |
| `solid-checker` | haiku | Low | Pattern-matching review |
| `code-improvement-advisor` | sonnet | Medium | Already exists, keep as-is |

**Design principle:** Implementation agents use `sonnet` (needs reasoning). Review agents use `haiku` (checking patterns, cheaper). Hooks use zero tokens.

---

## 10. Implementation Order

Build in this order to get value incrementally:

1. **Hooks** (settings.json) — immediate value, no new files needed beyond config
2. **`codebase-investigator` agent** — enables improved `/investigate` workflow
3. **`solid-checker` agent** — enables review workflow
4. **`/solid-review` skill** — ties into `solid-checker` + existing agents
5. **`revit-api-expert` agent** — domain-specific implementation + review
6. **`wpf-reviewer` agent** — complements existing `senior-wpf-developer`
7. **`/wpf-new` skill + templates** — scaffolding, most template work
8. **`/revit-new` skill + templates** — scaffolding, Revit-specific

---

## 11. Success Criteria

- [ ] `dotnet build` failure blocks commits across all projects
- [ ] Framework is auto-detected and visible to Claude at session start
- [ ] `/wpf-new view MyControl` generates a MVVM-compliant UserControl + ViewModel
- [ ] `/revit-new command MyCommand` generates a Transaction-safe ExternalCommand
- [ ] `/solid-review` produces actionable findings with file:line references
- [ ] `/build` parses errors and suggests fixes
- [ ] Commit messages follow conventional format (warned if not)
- [ ] Large diffs trigger a split warning
- [ ] Agents produce focused, non-overlapping analysis
- [ ] All hooks can be disabled by renaming the `hooks` key in settings.json
