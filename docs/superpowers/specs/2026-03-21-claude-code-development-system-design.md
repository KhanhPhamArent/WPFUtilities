# Claude Code Development System for C#/WPF/Revit Add-in Projects

**Date:** 2026-03-21
**Scope:** Global setup across all C#/WPF/Revit Add-in projects
**Target workflow:** Investigate → Plan → Implement → Build → Test in Revit → Commit → PR

---

## 1. Overview

A layered automation system for Claude Code, tailored to C# WPF Revit Add-in development. Three layers work together, each doing what it does best:

- **Hooks** — hard gates that enforce rules automatically (build must pass, commits must be clean)
- **Skills** — phase-specific guidance invoked via slash commands (scaffolding, review, research)
- **Agents** — specialized subagents dispatched behind the scenes for focused analysis

The system is framework-aware (supports .NET Framework 4.8 for Revit 2024 and .NET 8.0-windows for Revit 2026), detected per-project from `.csproj` files.

---

## 2. Hooks

All hooks are defined in the global `~/.claude/settings.json` and apply to every project.

### 2.1 Build Gate (PreCommit)

- **Trigger:** Before every commit
- **Action:** Runs `dotnet build` against the project/solution in the working directory
- **On failure:** Blocks the commit, displays build errors
- **Implementation:** Shell script that finds the nearest `.csproj` or `.sln` and builds it

```jsonc
// Hook definition
{
  "event": "PreCommit",
  "command": "bash -c 'PROJECT=$(find . -maxdepth 2 -name \"*.sln\" -o -name \"*.csproj\" | head -1) && dotnet build \"$PROJECT\" --no-restore 2>&1 | tail -20'",
  "blocking": true
}
```

### 2.2 Framework Detector (SessionStart)

- **Trigger:** At session start
- **Action:** Reads `.csproj` `<TargetFramework>` element, outputs the detected framework(s)
- **Purpose:** Claude sees the framework in context and adjusts API suggestions accordingly (e.g., `IAsyncDisposable` on .NET 8.0, not on 4.8)

```jsonc
{
  "event": "SessionStart",
  "command": "bash -c 'grep -rh \"<TargetFramework\" --include=\"*.csproj\" . 2>/dev/null | head -5 || echo \"No .csproj found\"'"
}
```

### 2.3 Commit Message Lint (PostCommit)

- **Trigger:** After every commit
- **Action:** Validates the most recent commit message against conventional commit format
- **Accepted prefixes:** `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `style:`, `test:`, `build:`, `perf:`
- **On failure:** Warns (non-blocking) — suggests amending the message

```jsonc
{
  "event": "PostCommit",
  "command": "bash -c 'MSG=$(git log -1 --pretty=%s) && echo \"$MSG\" | grep -qE \"^(feat|fix|refactor|chore|docs|style|test|build|perf)(\\(.+\\))?:\" || echo \"WARNING: Commit message does not follow conventional format. Expected: type(scope): description\"'"
}
```

### 2.4 Diff Size Warning (PreCommit)

- **Trigger:** Before every commit
- **Action:** Counts lines in staged diff. Warns if >500 lines.
- **On failure:** Non-blocking warning suggesting to split the commit

```jsonc
{
  "event": "PreCommit",
  "command": "bash -c 'LINES=$(git diff --cached --stat | tail -1 | grep -oE \"[0-9]+ insertion\" | grep -oE \"[0-9]+\"); if [ \"${LINES:-0}\" -gt 500 ]; then echo \"WARNING: Staged diff is ${LINES} lines. Consider splitting into smaller commits.\"; fi'"
}
```

---

## 3. Skills (Slash Commands)

Skills are stored in `~/.claude/skills/` and invoked via slash commands. Each maps to a specific phase of the development workflow.

### 3.1 `/wpf-new` — WPF Scaffold

- **Phase:** Implement
- **Purpose:** Generates boilerplate for common WPF/MVVM patterns
- **Subcommands (via argument):**
  - `view` — UserControl + ViewModel pair with INotifyPropertyChanged
  - `command` — ICommand implementation (RelayCommand pattern)
  - `converter` — IValueConverter with proper null handling
  - `behavior` — Microsoft.Xaml.Behaviors behavior class
  - `dialog` — Window + ViewModel for modal dialogs
- **Conventions enforced:**
  - ViewModels in `ViewModels/` folder, Views in `Views/` folder
  - Nullable reference types enabled
  - No code-behind logic — all in ViewModel
  - C# 12 syntax (primary constructors, collection expressions where appropriate)

### 3.2 `/revit-new` — Revit Command Scaffold

- **Phase:** Implement
- **Purpose:** Generates boilerplate for Revit API patterns
- **Subcommands (via argument):**
  - `command` — IExternalCommand implementation with Transaction handling
  - `event` — IExternalEventHandler with ExternalEvent wrapper
  - `updater` — IUpdater implementation with trigger registration
  - `panel` — IDockablePaneProvider with WPF host
  - `application` — IExternalApplication with ribbon setup
- **Framework-aware:** Detects target framework from `.csproj` and adjusts:
  - .NET 4.8: `TaskDialog`, classic Transaction patterns
  - .NET 8.0: async patterns where supported, nullable annotations
- **Conventions enforced:**
  - Transaction wrapped in `using` with explicit `Start()`/`Commit()`
  - Error handling with `TaskDialog` for user-facing errors
  - Follows Revit API best practices (no modeless operations in command context, etc.)

### 3.3 `/solid-review` — SOLID/Clean Code Review

- **Phase:** Pre-commit review
- **Purpose:** Reviews changed files against Clean Code and SOLID principles
- **Behavior:**
  1. Runs `git diff --cached` (or `git diff` if nothing staged) to identify changed files
  2. Dispatches `solid-checker` agent on each changed `.cs` file
  3. Dispatches `wpf-reviewer` agent on changed `.xaml`/ViewModel files
  4. Dispatches `revit-api-expert` agent on files touching Revit API types
  5. Aggregates findings into a single report with severity levels:
     - **Error** — must fix before commit (e.g., Transaction not disposed)
     - **Warning** — should fix (e.g., SRP violation, method too long)
     - **Info** — suggestion (e.g., could extract interface)
- **Output:** Concise, actionable findings with file:line references

### 3.4 `/build` — Build & Validate

- **Phase:** Build
- **Purpose:** Runs build, parses output, suggests fixes
- **Behavior:**
  1. Detects `.csproj`/`.sln` in working directory
  2. Runs `dotnet build`
  3. On failure: parses MSBuild errors, suggests fixes for common issues:
     - Missing NuGet references
     - Framework compatibility issues (4.8 vs 8.0 API differences)
     - Revit API version mismatches
  4. On success: reports warnings, suggests suppressions if appropriate

---

## 4. Agents (Subagents)

Agents are defined in `~/.claude/agents/` and dispatched by skills or by Claude when contextually relevant. Users do not invoke these directly.

### 4.1 `revit-api-expert`

- **Dispatched when:** Code touches Revit API types (Document, Element, Transaction, FilteredElementCollector, etc.)
- **Responsibilities:**
  - Reviews Transaction lifecycle (Start/Commit/Dispose, nested transactions)
  - Checks for common pitfalls: accessing elements outside valid context, modifying read-only parameters, wrong BuiltInParameter usage
  - Validates event handler context (modeless vs modal, ExternalEvent usage)
  - Flags deprecated Revit API calls per target version
  - Framework-aware: different patterns for 4.8 vs 8.0
- **Tools available:** Read, Grep, Glob, WebSearch, WebFetch

### 4.2 `wpf-reviewer`

- **Dispatched when:** `.xaml` or ViewModel files are modified
- **Responsibilities:**
  - Checks for MVVM violations (logic in code-behind, event handlers instead of commands)
  - Validates data binding patterns (missing INotifyPropertyChanged, incorrect binding paths)
  - Reviews visual tree structure for performance issues
  - Checks resource dictionary usage and style consistency
  - Validates DependencyProperty declarations (correct metadata, callbacks)
- **Tools available:** Read, Grep, Glob

### 4.3 `build-validator`

- **Dispatched when:** Before commit (by `/solid-review` or commit workflow)
- **Isolation:** Runs in a git worktree to avoid interfering with working directory
- **Responsibilities:**
  - Runs `dotnet build` in clean worktree
  - Reports build errors and warnings
  - Suggests fixes for common build failures
- **Tools available:** Bash, Read, Glob

### 4.4 `solid-checker`

- **Dispatched when:** During code review phase
- **Responsibilities:**
  - Analyzes changed `.cs` files for SOLID violations:
    - **SRP:** Class/method doing more than one thing, method >20 lines, class >200 lines
    - **OCP:** Switch statements on type, hardcoded dependencies
    - **LSP:** Derived types that throw NotImplementedException, weakened preconditions
    - **ISP:** Interfaces with >5 methods, unused interface members
    - **DIP:** Direct `new` of dependencies (vs injection), static service locator usage
  - Clean Code checks: magic numbers, unclear names, deep nesting (>3 levels)
  - Returns findings with severity, file:line, and suggested fix
- **Tools available:** Read, Grep, Glob

---

## 5. File Structure

```
~/.claude/
├── CLAUDE.md                          # Global instructions (already exists)
├── settings.json                      # Hooks + permissions (update existing)
├── agents/
│   ├── revit-api-expert.md
│   ├── wpf-reviewer.md
│   ├── build-validator.md
│   └── solid-checker.md
└── commands/
    ├── wpf-new.md
    ├── revit-new.md
    ├── solid-review.md
    └── build.md
```

---

## 6. What Already Exists (No Work Needed)

These are already available via the superpowers plugin and should be used as-is:

| Component | Type | Phase |
|---|---|---|
| `investigating-problem` | Skill | Investigate |
| `revit-api-research` | Skill | Investigate |
| `superpowers:brainstorming` | Skill | Plan |
| `superpowers:writing-plans` | Skill | Plan |
| `superpowers:test-driven-development` | Skill | Implement |
| `superpowers:systematic-debugging` | Skill | Debug |
| `senior-wpf-developer` | Agent | Implement |
| `code-improvement-advisor` | Agent | Review |
| `superpowers:requesting-code-review` | Agent | Review |
| `commit` | Skill | Commit |
| `create-pr` | Skill | PR |
| `superpowers:verification-before-completion` | Skill | Pre-commit |

---

## 7. Integration Points

### Development Flow Chain

```
Investigate ──→ Plan ──→ Implement ──→ Build ──→ Review ──→ Commit ──→ PR
    │              │          │            │          │          │        │
    ▼              ▼          ▼            ▼          ▼          ▼        ▼
/investigate  brainstorm  /wpf-new    /build    /solid-review /commit /create-pr
/revit-research plans     /revit-new  hook:build  agents:     hook:    existing
               existing   senior-wpf  validator   solid+wpf+  build+
                          agent                   revit        lint+diff
```

### Hook → Skill → Agent Dispatch Chain

1. **SessionStart hook** detects framework → informs all subsequent skill/agent behavior
2. **User invokes skill** (e.g., `/solid-review`) → skill dispatches relevant agents in parallel
3. **PreCommit hooks** run as final gate → build must pass, diff size checked
4. **PostCommit hook** validates commit message format

---

## 8. Success Criteria

- [ ] `dotnet build` failure blocks commits across all projects
- [ ] Framework is auto-detected and visible to Claude at session start
- [ ] `/wpf-new view MyControl` generates a MVVM-compliant UserControl + ViewModel
- [ ] `/revit-new command MyCommand` generates a Transaction-safe ExternalCommand
- [ ] `/solid-review` produces actionable findings with file:line references
- [ ] `/build` parses errors and suggests fixes
- [ ] Commit messages follow conventional format (warned if not)
- [ ] Large diffs trigger a split warning
- [ ] All agents produce focused, non-overlapping analysis
