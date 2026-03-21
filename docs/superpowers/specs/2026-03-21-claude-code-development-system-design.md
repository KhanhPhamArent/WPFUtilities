# Claude Code Development System for C#/WPF/Revit Add-in Projects

**Date:** 2026-03-21
**Scope:** Global setup across all C#/WPF/Revit Add-in projects
**Target workflow:** Investigate -> Plan -> Implement -> Build -> Test in Revit -> Commit -> PR

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

### 3.4 `/build` — Build & Validate

**Location:** `~/.claude/skills/build/SKILL.md`

- **Phase:** Build
- **Purpose:** Runs build, parses output, suggests fixes
- **Dynamic context:** `!`grep -rh "<TargetFramework" --include="*.csproj" . 2>/dev/null``
- **Instructions to Claude:**
  1. Find `.csproj`/`.sln` in working directory
  2. Run `dotnet build` via Bash
  3. On failure: parse MSBuild errors, suggest fixes for common issues (missing NuGet refs, framework compat, Revit API version mismatches)
  4. On success: report warnings count, suggest suppressions if appropriate

**Note:** The `/build` skill is the intelligent build tool (diagnoses and suggests fixes). The PreToolUse build hook (2.1) is a thin pass/fail gate. They serve different purposes — the hook prevents bad commits, the skill helps you fix build problems.

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

### 4.1 `revit-api-expert`

- **Model:** sonnet
- **Color:** yellow
- **Triggered when:** Code touches Revit API types (Document, Element, Transaction, FilteredElementCollector, etc.)
- **Responsibilities:**
  - Reviews Transaction lifecycle (Start/Commit/Dispose, nested transactions)
  - Checks for common pitfalls: accessing elements outside valid context, modifying read-only parameters, wrong BuiltInParameter usage
  - Validates event handler context (modeless vs modal, ExternalEvent usage)
  - Flags deprecated Revit API calls per target version
  - Framework-aware: different patterns for 4.8 vs 8.0
- **Tools:** Read, Grep, Glob, WebSearch, WebFetch

### 4.2 `wpf-reviewer`

- **Model:** sonnet
- **Color:** cyan
- **Triggered when:** `.xaml` or ViewModel files are modified
- **Responsibilities:**
  - Checks for MVVM violations (logic in code-behind, event handlers instead of commands)
  - Validates data binding patterns (missing INotifyPropertyChanged, incorrect binding paths)
  - Reviews visual tree structure for performance issues
  - Checks resource dictionary usage and style consistency
  - Validates DependencyProperty declarations (correct metadata, callbacks)
- **Tools:** Read, Grep, Glob

### 4.3 `solid-checker`

- **Model:** sonnet
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
    ├── solid-review/
    │   └── SKILL.md                             # NEW
    └── build/
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
Investigate ──> Plan ──> Implement ──> Build ──> Review ──> Commit ──> PR
    |              |          |            |          |          |        |
    v              v          v            v          v          v        v
/investigate  brainstorm  /wpf-new    /build    /solid-review /commit /create-pr
/revit-       writing-    /revit-new  build     solid-checker  build   existing
 research     plans       senior-wpf  hook      wpf-reviewer   hook
              existing    agent       (gate)    revit-expert   commit
                                                code-improv.   lint
                                                advisor        diff warn
```

### How the layers interact

1. **SessionStart hook** detects framework -> visible to Claude for all subsequent work
2. **User invokes skill** (e.g., `/solid-review`) -> skill instructions guide Claude to read changed files and consider dispatching relevant agents
3. **Claude dispatches agents** based on file types and content (Revit API types -> `revit-api-expert`, XAML -> `wpf-reviewer`, C# -> `solid-checker`)
4. **PreToolUse hook on Bash** -> when `git commit` is detected, build gate runs and diff size is checked
5. **PostToolUse hook on Bash** -> after `git commit` succeeds, commit message format is validated

---

## 9. Implementation Order

Build in this order to get value incrementally:

1. **Hooks** (settings.json) — immediate value, no new files needed beyond config
2. **`/build` skill** — most frequently used, simplest skill
3. **`solid-checker` agent** — enables review workflow
4. **`/solid-review` skill** — ties into `solid-checker` + existing agents
5. **`revit-api-expert` agent** — domain-specific value
6. **`wpf-reviewer` agent** — complements existing `senior-wpf-developer`
7. **`/wpf-new` skill + templates** — scaffolding, most template work
8. **`/revit-new` skill + templates** — scaffolding, Revit-specific

---

## 10. Success Criteria

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
