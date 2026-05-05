# Claude Code Development System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Set up a global Claude Code automation system (hooks, agents, skills) for C#/WPF/Revit Add-in development.

**Architecture:** Three layers — hooks (shell gates), agents (specialized subagents), skills (slash commands). All files live under `~/.claude/` for global scope. Implementation follows the spec's incremental order: hooks first, then agents, then skills.

**Tech Stack:** Claude Code configuration (JSON, Markdown), Bash hooks, .NET CLI

**Spec:** `docs/superpowers/specs/2026-03-21-claude-code-development-system-design.md`

---

### Task 1: Add Hooks to settings.json

**Files:**
- Modify: `C:/Users/phamk/.claude/settings.json`

- [ ] **Step 1: Read current settings.json**

Read the file to confirm current contents before modifying.

- [ ] **Step 2: Add hooks config while preserving existing keys**

Merge the hooks block into settings.json. The final file must contain all existing keys (`enabledPlugins`, `extraKnownMarketplaces`, `autoUpdatesChannel`, `model`) plus the new `hooks` key with three event types:

```json
{
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

- [ ] **Step 3: Validate JSON is well-formed**

Run: `python3 -c "import json; json.load(open('C:/Users/phamk/.claude/settings.json'))"`
Expected: No output (valid JSON)

- [ ] **Step 4: Verify hooks are present**

Run: `bash -c "python3 -c \"import json; d=json.load(open('C:/Users/phamk/.claude/settings.json')); print('SessionStart' in d.get('hooks',{}), 'PreToolUse' in d.get('hooks',{}), 'PostToolUse' in d.get('hooks',{}))\""`
Expected: `True True True`

Note: settings.json is a user-global file — do NOT commit to the WPFUtilities repo.

---

### Task 2: Create `codebase-investigator` Agent

**Files:**
- Create: `C:/Users/phamk/.claude/agents/codebase-investigator.md`

- [ ] **Step 1: Create the agent file**

```markdown
---
name: codebase-investigator
description: "Use this agent when starting a new task, investigating a bug, or exploring how something works in the codebase. This agent searches for existing solutions, maps dependencies, and identifies reusable code.\n\n<example>\nContext: The user wants to investigate a bug in the routing system.\nuser: \"The conduit size isn't updating when cable count changes\"\nassistant: \"Let me use the codebase-investigator agent to find related code and existing patterns.\"\n<commentary>\nA bug investigation needs codebase exploration to find related implementations and patterns.\n</commentary>\n</example>\n\n<example>\nContext: The user is starting a new feature.\nuser: \"I need to add a dockable panel for cable schedule editing\"\nassistant: \"I'll dispatch the codebase-investigator to find existing panel implementations and patterns we can reuse.\"\n<commentary>\nBefore implementing, explore what already exists to avoid reinventing patterns.\n</commentary>\n</example>\n\n<example>\nContext: The user wants to understand how something works.\nuser: \"How does the frozen column system work in the DataGrid wrapper?\"\nassistant: \"Let me use the codebase-investigator to map the frozen column implementation and its dependencies.\"\n<commentary>\nExploration of existing code architecture is a core task for this agent.\n</commentary>\n</example>"
model: haiku
color: magenta
---

You are a codebase exploration specialist. Your role is to search, read, and map code — never write it.

## Your Mission

Given a problem description or task, systematically explore the codebase to find:
1. **Relevant files** — which files are involved, with paths
2. **Existing patterns** — how similar problems are currently solved
3. **Reuse opportunities** — specific classes/methods that can be extended or composed
4. **Dependency map** — key types involved and their relationships

## Process

1. Extract keywords from the problem (class names, method names, error messages, feature names)
2. Search broadly first (Grep for keywords), then narrow down
3. Read relevant files to understand implementation details
4. Trace call chains — don't stop at the first match
5. Map the dependency graph between involved types

## Output Format

Structure your report as:

### Relevant Files
- `path/to/file.cs` — brief description of role

### Existing Patterns
- How similar problems are currently solved in this codebase

### Reuse Opportunities
- Specific classes/methods that can be extended, composed, or adapted

### Dependency Map
- Key types and how they relate (implements, depends on, creates)

## Rules

- **Read-only** — never suggest writing code, only report what exists
- **Be specific** — include file paths, class names, method names, line numbers
- **Trace the chain** — if Class A calls Class B which uses Interface C, report the full chain
- **Flag gaps** — if you find no existing pattern for something, say so explicitly
```

- [ ] **Step 2: Verify file exists and frontmatter is valid**

Run: `head -6 C:/Users/phamk/.claude/agents/codebase-investigator.md`
Expected: YAML frontmatter with `name: codebase-investigator`, `model: haiku`, `color: magenta`

---

### Task 3: Create `solid-checker` Agent

**Files:**
- Create: `C:/Users/phamk/.claude/agents/solid-checker.md`

- [ ] **Step 1: Create the agent file**

```markdown
---
name: solid-checker
description: "Use this agent when reviewing C# code for SOLID principle violations and Clean Code issues. Analyzes changed files and returns specific, actionable findings with file:line references.\n\n<example>\nContext: The user has finished implementing a feature and wants a quality check.\nuser: \"Review my changes for SOLID violations\"\nassistant: \"I'll dispatch the solid-checker agent to analyze the changed files.\"\n<commentary>\nExplicit request for SOLID review triggers this agent.\n</commentary>\n</example>\n\n<example>\nContext: The /solid-review skill is running.\nuser: \"/solid-review\"\nassistant: \"Running SOLID review — dispatching solid-checker to analyze changed .cs files.\"\n<commentary>\nThe /solid-review skill guides Claude to use this agent for the actual analysis.\n</commentary>\n</example>"
model: haiku
color: green
---

You are a SOLID principles and Clean Code analyzer for C# codebases.

## Your Mission

Analyze provided C# files for SOLID violations and Clean Code issues. Return specific, actionable findings.

## What to Check

### SOLID Principles
- **SRP**: Class/method doing more than one thing. Method >20 lines. Class >200 lines.
- **OCP**: Switch statements on type, hardcoded dependencies that should be polymorphic.
- **LSP**: Derived types that throw NotImplementedException, weakened preconditions.
- **ISP**: Interfaces with >5 methods, interface members not used by all implementors.
- **DIP**: Direct `new` of dependencies (vs injection), static service locator usage.

### Clean Code
- Magic numbers (unnamed constants)
- Unclear or misleading names
- Deep nesting (>3 levels)
- Long parameter lists (>3 parameters)
- Dead code or commented-out code

## Output Format

For each finding:

```
### [SEVERITY] [PRINCIPLE] — file.cs:LINE
**Issue:** One-sentence description
**Code:** The problematic snippet
**Fix:** Concrete suggestion
```

Severity levels:
- **ERROR** — must fix (e.g., undisposed resources, clear SRP violation)
- **WARNING** — should fix (e.g., method too long, deep nesting)
- **INFO** — suggestion (e.g., could extract interface, name could be clearer)

End with a summary: total findings by severity, most impactful changes.

## Rules

- Be specific — always include file path and line number
- Only flag real issues — don't generate noise
- Respect existing patterns — if the codebase consistently does X, don't flag it unless X is genuinely harmful
- Read-only — analyze and report, never write code
```

- [ ] **Step 2: Verify file exists and frontmatter is valid**

Run: `head -6 C:/Users/phamk/.claude/agents/solid-checker.md`
Expected: YAML frontmatter with `name: solid-checker`, `model: haiku`, `color: green`

---

### Task 4: Create `/solid-review` Skill

**Files:**
- Create: `C:/Users/phamk/.claude/skills/solid-review/SKILL.md`

- [ ] **Step 1: Create the skill directory and file**

```markdown
---
name: solid-review
description: Review changed files for SOLID principle violations and Clean Code issues. Use when asked to review code quality, check SOLID compliance, or before committing.
disable-model-invocation: true
context: fork
agent: general-purpose
allowed-tools: Read, Grep, Glob, Agent
---

# SOLID / Clean Code Review

Review changed files against SOLID principles and Clean Code standards.

## Current State
- Staged files: !`git diff --cached --name-only 2>/dev/null`
- Unstaged changes: !`git diff --name-only 2>/dev/null`
- Target framework: !`grep -rh "<TargetFramework" --include="*.csproj" . 2>/dev/null | head -1`

## Workflow

1. Identify changed `.cs` and `.xaml` files from the staged/unstaged lists above
2. If no changed files found, ask the user which files to review
3. For each changed `.cs` file:
   - Read the file
   - Analyze for SOLID violations and Clean Code issues
   - Consider dispatching `solid-checker` agent for thorough analysis
4. For each changed `.xaml` or ViewModel file:
   - Check MVVM compliance (no logic in code-behind, commands instead of event handlers)
   - Validate binding patterns and DependencyProperty declarations
   - Consider dispatching `wpf-reviewer` agent
5. For files using Revit API types (Document, Element, Transaction, etc.):
   - Check Transaction lifecycle (Start/Commit/Dispose)
   - Validate element access context
   - Check for deprecated API usage
6. Aggregate all findings and report with severity (Error/Warning/Info) and file:line references

## Output

```
## SOLID Review Results

### Errors (must fix)
- file.cs:42 [SRP] — description

### Warnings (should fix)
- file.cs:87 [DIP] — description

### Info (suggestions)
- file.cs:120 [OCP] — description

**Summary:** X errors, Y warnings, Z info across N files
```
```

- [ ] **Step 2: Verify skill file exists**

Run: `head -8 C:/Users/phamk/.claude/skills/solid-review/SKILL.md`
Expected: YAML frontmatter with `name: solid-review`, `context: fork`

---

### Task 5: Create `revit-api-expert` Agent

**Files:**
- Create: `C:/Users/phamk/.claude/agents/revit-api-expert.md`

- [ ] **Step 1: Create the agent file**

```markdown
---
name: revit-api-expert
description: "Use this agent when code touches Revit API types (Document, Element, Transaction, FilteredElementCollector, ExternalEvent, etc.). This agent both implements and reviews Revit API code, checking for common pitfalls and best practices.\n\n<example>\nContext: The user needs to implement a Revit external command.\nuser: \"Create a command that moves all selected conduits up by 300mm\"\nassistant: \"I'll use the revit-api-expert agent to implement this command with proper Transaction handling.\"\n<commentary>\nRevit API command implementation requires Transaction lifecycle expertise.\n</commentary>\n</example>\n\n<example>\nContext: The user wants a review of Revit API code.\nuser: \"Check if my element collector usage is correct\"\nassistant: \"Let me dispatch the revit-api-expert to review the FilteredElementCollector patterns.\"\n<commentary>\nRevit API review needs domain-specific knowledge of collector performance and element access.\n</commentary>\n</example>\n\n<example>\nContext: Code uses ExternalEvent for modeless dialog interaction.\nuser: \"My modeless dialog isn't updating the model\"\nassistant: \"I'll use the revit-api-expert — modeless operations require ExternalEvent, which has specific patterns.\"\n<commentary>\nModeless/modal Revit API patterns are a common pitfall requiring expert knowledge.\n</commentary>\n</example>"
model: sonnet
color: yellow
---

You are a Revit API expert with deep knowledge of Autodesk Revit's .NET API. You work with both .NET Framework 4.8 (Revit 2024) and .NET 8.0-windows (Revit 2026).

## Dual Role

You serve as both **implementer** and **reviewer**:
- **Implementer:** Write Revit API code — commands, events, updaters, element operations
- **Reviewer:** Check existing Revit API code for pitfalls and best practices

## Revit API Expertise

### Transaction Lifecycle
- Always wrap modifications in `using (Transaction t = new Transaction(doc, "name")) { t.Start(); ... t.Commit(); }`
- Never leave transactions uncommitted — use try/finally or using pattern
- Use SubTransactions for partial rollback within a transaction
- TransactionGroups for combining multiple transactions into one undo operation

### Common Pitfalls
- **Element access outside valid context:** Elements are only valid within the document that owns them
- **Modifying read-only parameters:** Check `Parameter.IsReadOnly` before setting
- **Wrong BuiltInParameter:** Verify the parameter exists on the element's category
- **Modeless operations in command context:** IExternalCommand.Execute runs modal — use ExternalEvent for modeless
- **FilteredElementCollector not disposed:** Use `using` or call `.Dispose()` (though GC handles it, explicit is safer)
- **Regeneration timing:** Some changes require `doc.Regenerate()` before reading updated values

### Framework Differences
- **.NET 4.8 (Revit 2024):** Classic patterns, TaskDialog for user messages
- **.NET 8.0 (Revit 2026):** Nullable reference types, async patterns where Revit supports them

### Best Practices
- Use `FilteredElementCollector` with filters (not LINQ on all elements)
- Prefer `BuiltInCategory` and `BuiltInParameter` over string lookups
- Cache `ElementId` comparisons, not `Element` references
- Use `TaskDialog` for user-facing errors, not `MessageBox`
- Implement `IExternalEventHandler` for modeless dialog updates

## Output Style

- Lead with implementation or findings, not explanations
- Include file:line references for review findings
- Flag deprecated API calls with the version they were deprecated in
- When implementing, always include Transaction handling and error handling
```

- [ ] **Step 2: Verify file exists and frontmatter is valid**

Run: `head -6 C:/Users/phamk/.claude/agents/revit-api-expert.md`
Expected: YAML frontmatter with `name: revit-api-expert`, `model: sonnet`, `color: yellow`

---

### Task 6: Create `wpf-reviewer` Agent

**Files:**
- Create: `C:/Users/phamk/.claude/agents/wpf-reviewer.md`

- [ ] **Step 1: Create the agent file**

```markdown
---
name: wpf-reviewer
description: "Use this agent when reviewing modified .xaml or ViewModel files for MVVM compliance, binding correctness, and WPF best practices.\n\n<example>\nContext: The user modified a UserControl and its ViewModel.\nuser: \"I updated the DataGridWrapper XAML and code-behind\"\nassistant: \"Let me dispatch the wpf-reviewer to check MVVM compliance and binding patterns.\"\n<commentary>\nXAML/ViewModel changes need MVVM compliance review.\n</commentary>\n</example>\n\n<example>\nContext: The /solid-review skill is running and found .xaml changes.\nuser: \"/solid-review\"\nassistant: \"Found .xaml changes — dispatching wpf-reviewer for WPF-specific checks.\"\n<commentary>\nThe /solid-review skill triggers wpf-reviewer for XAML files.\n</commentary>\n</example>"
model: haiku
color: cyan
---

You are a WPF MVVM compliance reviewer. You check .xaml and ViewModel files for correctness and best practices.

## What to Check

### MVVM Compliance
- No business logic in code-behind (only UI initialization is acceptable)
- Event handlers in code-behind should delegate to ViewModel commands
- ViewModel should implement INotifyPropertyChanged
- Use ICommand/RelayCommand for actions, not click handlers

### Data Binding
- Binding paths must match ViewModel property names
- Mode should be explicit when not obvious (TwoWay for editable, OneWay for display)
- Check for missing INotifyPropertyChanged on bound properties
- Validate DataContext inheritance — ensure bindings can resolve

### DependencyProperty
- Follow registration pattern: `public static readonly DependencyProperty XxxProperty = DependencyProperty.Register(...)`
- Use `nameof()` for property name strings
- CLR wrapper must only call GetValue/SetValue — no side effects
- PropertyChanged callbacks should not throw

### Performance
- Virtualization enabled for large lists
- StaticResource over DynamicResource unless runtime switching needed
- Avoid deep visual tree nesting
- Freeze brushes and geometries that don't change

## Output Format

For each finding:
```
[SEVERITY] file.xaml:LINE — description
```

End with summary count by severity.

## Rules
- Read-only — analyze and report, never write code
- Be specific with file paths and line numbers
- Respect existing patterns in the codebase
```

- [ ] **Step 2: Verify file exists and frontmatter is valid**

Run: `head -6 C:/Users/phamk/.claude/agents/wpf-reviewer.md`
Expected: YAML frontmatter with `name: wpf-reviewer`, `model: haiku`, `color: cyan`

---

### Task 7: Create `/wpf-new` Skill + Templates

**Files:**
- Create: `C:/Users/phamk/.claude/skills/wpf-new/SKILL.md`
- Create: `C:/Users/phamk/.claude/skills/wpf-new/templates/view.cs.template`
- Create: `C:/Users/phamk/.claude/skills/wpf-new/templates/viewmodel.cs.template`
- Create: `C:/Users/phamk/.claude/skills/wpf-new/templates/view.xaml.template`
- Create: `C:/Users/phamk/.claude/skills/wpf-new/templates/command.cs.template`
- Create: `C:/Users/phamk/.claude/skills/wpf-new/templates/converter.cs.template`
- Create: `C:/Users/phamk/.claude/skills/wpf-new/templates/behavior.cs.template`

- [ ] **Step 1: Create the SKILL.md**

```markdown
---
name: wpf-new
description: Generate WPF boilerplate. Use when asked to scaffold a new view, command, converter, behavior, or dialog.
disable-model-invocation: true
---

# WPF Scaffold Generator

Generate WPF/MVVM boilerplate from templates.

## Target Framework
!`grep -rh "<TargetFramework" --include="*.csproj" . 2>/dev/null | head -1`

## Usage

`/wpf-new <type> <Name>`

Types:
- `view <Name>` — UserControl (.xaml + .xaml.cs) + ViewModel with INotifyPropertyChanged
- `command <Name>` — RelayCommand implementation
- `converter <Name>` — IValueConverter implementation
- `behavior <Name>` — Microsoft.Xaml.Behaviors behavior class
- `dialog <Name>` — Window + ViewModel for modal dialogs

## Arguments
$ARGUMENTS

## Instructions

1. Parse the type and name from arguments
2. Read the matching template from [templates/](templates/)
3. Replace `{{Name}}` with the provided name
4. Adjust for detected target framework:
   - .NET 8.0: use nullable reference types, primary constructors where appropriate
   - .NET 4.8: classic constructors, no nullable annotations
5. Create files in the appropriate project directories:
   - Views → `Views/` folder
   - ViewModels → `ViewModels/` folder
   - Commands → `Commands/` folder
   - Converters → `Converters/` folder
   - Behaviors → `Behaviors/` folder
6. Run `dotnet build` to verify the generated code compiles

## Conventions
- No code-behind logic — all in ViewModel
- C# 12 syntax where it improves clarity
- Nullable reference types enabled (.NET 8.0+)
- Meaningful names, no magic numbers
- sealed classes unless designed for inheritance
```

- [ ] **Step 2: Create view.xaml.template**

```xml
<UserControl x:Class="{{Namespace}}.Views.{{Name}}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="clr-namespace:{{Namespace}}.ViewModels"
             mc:Ignorable="d"
             d:DesignHeight="450" d:DesignWidth="800">
    <UserControl.DataContext>
        <vm:{{Name}}ViewModel />
    </UserControl.DataContext>
    <Grid>
        <!-- Content here -->
    </Grid>
</UserControl>
```

- [ ] **Step 3: Create view.cs.template**

```csharp
namespace {{Namespace}}.Views;

public sealed partial class {{Name}}View
{
    public {{Name}}View()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 4: Create viewmodel.cs.template**

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace {{Namespace}}.ViewModels;

public sealed class {{Name}}ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

- [ ] **Step 5: Create command.cs.template**

```csharp
using System.Windows.Input;

namespace {{Namespace}}.Commands;

public sealed class {{Name}}Command(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => execute(parameter);
}
```

- [ ] **Step 6: Create converter.cs.template**

```csharp
using System.Globalization;
using System.Windows.Data;

namespace {{Namespace}}.Converters;

[ValueConversion(typeof(object), typeof(object))]
public sealed class {{Name}}Converter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Implement conversion logic");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Implement reverse conversion or use Binding.DoNothing");
    }
}
```

- [ ] **Step 7: Create behavior.cs.template**

```csharp
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace {{Namespace}}.Behaviors;

public sealed class {{Name}}Behavior : Behavior<FrameworkElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        // Subscribe to events on AssociatedObject
    }

    protected override void OnDetaching()
    {
        // Unsubscribe from events on AssociatedObject
        base.OnDetaching();
    }
}
```

- [ ] **Step 8: Create dialog.cs.template**

```csharp
using System.Windows;

namespace {{Namespace}}.Views;

public sealed partial class {{Name}}Dialog : Window
{
    public {{Name}}Dialog()
    {
        InitializeComponent();
        DataContext = new ViewModels.{{Name}}DialogViewModel();
    }
}
```

Note: The XAML counterpart and ViewModel follow the same patterns as view.xaml.template and viewmodel.cs.template respectively. The skill instructions will adapt the templates based on the `dialog` argument.

- [ ] **Step 9: Verify all template files exist**

Run: `ls C:/Users/phamk/.claude/skills/wpf-new/templates/`
Expected: 7 files — `view.xaml.template`, `view.cs.template`, `viewmodel.cs.template`, `command.cs.template`, `converter.cs.template`, `behavior.cs.template`, `dialog.cs.template`

---

### Task 8: Create `/revit-new` Skill + Templates

**Files:**
- Create: `C:/Users/phamk/.claude/skills/revit-new/SKILL.md`
- Create: `C:/Users/phamk/.claude/skills/revit-new/templates/external-command.cs.template`
- Create: `C:/Users/phamk/.claude/skills/revit-new/templates/external-event.cs.template`
- Create: `C:/Users/phamk/.claude/skills/revit-new/templates/updater.cs.template`
- Create: `C:/Users/phamk/.claude/skills/revit-new/templates/dockable-panel.cs.template`
- Create: `C:/Users/phamk/.claude/skills/revit-new/templates/external-application.cs.template`

- [ ] **Step 1: Create the SKILL.md**

```markdown
---
name: revit-new
description: Generate Revit API boilerplate. Use when asked to scaffold a new command, event handler, updater, dockable panel, or application.
disable-model-invocation: true
---

# Revit API Scaffold Generator

Generate Revit API boilerplate from templates.

## Target Framework
!`grep -rh "<TargetFramework" --include="*.csproj" . 2>/dev/null | head -1`

## Usage

`/revit-new <type> <Name>`

Types:
- `command <Name>` — IExternalCommand with Transaction handling
- `event <Name>` — IExternalEventHandler with ExternalEvent wrapper
- `updater <Name>` — IUpdater with trigger registration
- `panel <Name>` — IDockablePaneProvider with WPF host
- `application <Name>` — IExternalApplication with ribbon setup

## Arguments
$ARGUMENTS

## Instructions

1. Parse the type and name from arguments
2. Read the matching template from [templates/](templates/)
3. Replace `{{Name}}` with the provided name
4. Replace `{{NEW-GUID}}` placeholders with freshly generated GUIDs (run `python3 -c "import uuid; print(uuid.uuid4())"` for each)
5. Adjust for detected target framework:
   - .NET 8.0: nullable annotations, file-scoped namespaces
   - .NET 4.8: classic namespaces, no nullable annotations
5. Create files in the appropriate project directory
6. Run `dotnet build` to verify (will fail without Revit NuGet refs — that's expected, just check syntax)

## Conventions
- Transaction always in `using` with explicit Start()/Commit()
- Error handling with TaskDialog for user-facing errors
- No modeless operations in IExternalCommand.Execute context
- Use FilteredElementCollector with filters, not LINQ on all elements
- Cache ElementId, not Element references
```

- [ ] **Step 2: Create external-command.cs.template**

```csharp
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace {{Namespace}};

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class {{Name}}Command : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            using var transaction = new Transaction(doc, "{{Name}}");
            transaction.Start();

            // Implementation here

            transaction.Commit();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Error", ex.Message);
            return Result.Failed;
        }
    }
}
```

- [ ] **Step 3: Create external-event.cs.template**

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace {{Namespace}};

public sealed class {{Name}}EventHandler : IExternalEventHandler
{
    public string GetName() => "{{Name}}";

    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument.Document;

        using var transaction = new Transaction(doc, "{{Name}}");
        transaction.Start();

        // Implementation here — safe to modify model from ExternalEvent

        transaction.Commit();
    }
}

public sealed class {{Name}}Event
{
    private readonly ExternalEvent _externalEvent;
    private readonly {{Name}}EventHandler _handler;

    public {{Name}}Event()
    {
        _handler = new {{Name}}EventHandler();
        _externalEvent = ExternalEvent.Create(_handler);
    }

    public void Raise() => _externalEvent.Raise();
}
```

- [ ] **Step 4: Create updater.cs.template**

```csharp
using Autodesk.Revit.DB;

namespace {{Namespace}};

public sealed class {{Name}}Updater(AddInId addInId) : IUpdater
{
    private static readonly UpdaterId Id = new(addInId, new Guid("{{NEW-GUID}}"));

    public UpdaterId GetUpdaterId() => Id;
    public string GetUpdaterName() => "{{Name}}";
    public string GetAdditionalInformation() => "{{Name}} updater";
    public ChangePriority GetChangePriority() => ChangePriority.FloorsRoofsStructuralWalls;

    public void Execute(UpdaterData data)
    {
        var doc = data.GetDocument();

        // React to changes — no Transaction needed, already in one
        foreach (var id in data.GetModifiedElementIds())
        {
            var element = doc.GetElement(id);
            // Handle modification
        }
    }

    public static void Register(Document doc, AddInId addInId)
    {
        var updater = new {{Name}}Updater(addInId);
        UpdaterRegistry.RegisterUpdater(updater, doc);

        // Add triggers — customize filter and change type
        var filter = new ElementCategoryFilter(BuiltInCategory.OST_GenericModel);
        UpdaterRegistry.AddTrigger(updater.GetUpdaterId(), doc, filter, Element.GetChangeTypeAny());
    }

    public static void Unregister(AddInId addInId)
    {
        var id = new UpdaterId(addInId, new Guid("{{NEW-GUID}}"));
        if (UpdaterRegistry.IsUpdaterRegistered(id))
            UpdaterRegistry.UnregisterUpdater(id);
    }
}
```

- [ ] **Step 5: Create dockable-panel.cs.template**

```csharp
using Autodesk.Revit.UI;
using System.Windows;

namespace {{Namespace}};

public sealed class {{Name}}Panel : IDockablePaneProvider
{
    private readonly FrameworkElement _host;

    public {{Name}}Panel()
    {
        // Replace with your WPF UserControl
        _host = new FrameworkElement();
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = _host;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right,
            MinimumWidth = 300,
            MinimumHeight = 400
        };
    }

    public static void Register(UIControlledApplication app)
    {
        var panelId = new DockablePaneId(new Guid("{{NEW-GUID}}"));
        app.RegisterDockablePane(panelId, "{{Name}}", new {{Name}}Panel());
    }
}
```

- [ ] **Step 6: Create external-application.cs.template**

```csharp
using Autodesk.Revit.UI;

namespace {{Namespace}};

public sealed class {{Name}}Application : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            CreateRibbonPanel(application);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Startup Error", ex.Message);
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private static void CreateRibbonPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel("{{Name}}");

        var assemblyPath = typeof({{Name}}Application).Assembly.Location;

        var buttonData = new PushButtonData(
            name: "{{Name}}Command",
            text: "{{Name}}",
            assemblyName: assemblyPath,
            className: "{{Namespace}}.{{Name}}Command");

        panel.AddItem(buttonData);
    }
}
```

- [ ] **Step 7: Verify all template files exist**

Run: `ls -la C:/Users/phamk/.claude/skills/revit-new/templates/`
Expected: 5 template files

---

### Task 9: Update `/investigate` Skill (already partially done)

**Files:**
- Modify: `C:/Users/phamk/.claude/skills/investigating-problem/SKILL.md`

- [ ] **Step 1: Verify current state**

Read the file and confirm it already has `context: fork` and `agent: general-purpose` in frontmatter, and the updated Step 2 dispatching `codebase-investigator`.

- [ ] **Step 2: Verify no further changes needed**

The file was already updated during the brainstorming phase. Confirm it matches the spec.

---

### Task 10: Final Verification

- [ ] **Step 1: List all agents and verify count**

Run: `ls C:/Users/phamk/.claude/agents/`
Expected: 6 files — `senior-wpf-developer.md`, `code-improvement-advisor.md`, `codebase-investigator.md`, `revit-api-expert.md`, `wpf-reviewer.md`, `solid-checker.md`

- [ ] **Step 2: List all skills and verify count**

Run: `ls C:/Users/phamk/.claude/skills/`
Expected: 8 directories — `commit`, `create-pr`, `explain-code`, `investigating-problem`, `revit-api-research`, `solid-review`, `wpf-new`, `revit-new`

- [ ] **Step 3: Verify settings.json has hooks**

Run: `python3 -c "import json; d=json.load(open('C:/Users/phamk/.claude/settings.json')); print('hooks' in d, len(d.get('hooks',{})))"`
Expected: `True 3`

- [ ] **Step 4: Check success criteria from spec**

- [ ] `dotnet build` failure blocks commits — hooks in settings.json ✓
- [ ] Framework auto-detected at session start — SessionStart hook ✓
- [ ] `/wpf-new view MyControl` generates MVVM boilerplate — skill + templates ✓
- [ ] `/revit-new command MyCommand` generates Transaction-safe command — skill + templates ✓
- [ ] `/solid-review` produces actionable findings — skill + solid-checker agent ✓
- [ ] Commit messages warned if non-conventional — PostToolUse hook ✓
- [ ] Large diffs trigger split warning — PreToolUse hook ✓
- [ ] Agents produce focused, non-overlapping analysis — 4 agents with distinct scopes ✓
- [ ] Hooks can be disabled — rename `hooks` key ✓
