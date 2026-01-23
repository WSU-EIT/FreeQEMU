# 005 Style Guides Index

> Category index for FreeCRM code style documentation.

**Source:** FreeCRM base template (public)

---

## ⚠️ CRITICAL: File Naming Convention

**All project-specific files MUST use the `{ProjectName}.App.{Feature}` naming pattern.**

This is mandatory, not optional. See `docs/004_styleguide.md` → "File Organization" for complete rules.

**Quick reference:**
```
{ProjectName}.App.{Feature}.{OptionalSub}.{Extension}

Examples:
FreeManager.App.EntityWizard.razor         # New page
FreeManager.App.EntityWizard.State.cs      # Partial (state mgmt)
FreeManager.App.DataObjects.Projects.cs    # DTOs
FreeManager.App.FMProject.cs               # Entity class
```

**Why:** Instant identification of your code vs base FreeCRM during framework updates.

---

## Quick Navigation

| Document | Description | When to Use |
|----------|-------------|-------------|
| [004_styleguide.md](004_styleguide.md) | **Complete style guide** | Primary reference |
| [005_style.comments.md](005_style.comments.md) | Comment patterns and voice | Writing code comments |

---

## Overview

The style guides establish consistent patterns for how code should be written and documented across all FreeCRM-based projects. These guides ensure that code is readable, maintainable, and follows established conventions.

---

## Key Style Topics

### File Naming (MANDATORY)
**Location:** `docs/004_styleguide.md` → "File Organization"

| Category | Pattern | Example |
|----------|---------|---------|
| New feature | `{Project}.App.{Feature}.cs` | `FreeManager.App.EntityWizard.cs` |
| Entity class | `{Project}.App.{Entity}.cs` | `FreeManager.App.FMProject.cs` |
| Partial split | `{Project}.App.{Feature}.{Sub}.cs` | `FreeManager.App.EntityWizard.State.cs` |
| Base extension | `{Base}.App.cs` | `DataController.App.cs` |

### Blazor Component Class Names
**Location:** `docs/004_styleguide.md` → "Blazor Component Naming"

Blazor converts dots to underscores:
- File: `FreeManager.App.EntityWizard.razor`
- Class: `FreeManager_App_EntityWizard`
- Reference: `<FreeManager_App_EntityWizard />`

---

## Available Guides

### 005_style.comments.md - Comment Style Guide

**Purpose:** Defines the voice, patterns, and formatting rules for code comments.

**Key Topics:**
- Comment voice characteristics (procedural, direct, present tense)
- Core patterns: "See if", "Make sure", "First/Now/Next"
- XML documentation standards
- What NOT to comment

**Use when:** Writing new code, reviewing PRs, or learning the project's comment conventions.

---

## Related Guides

| Guide | Location | Topic |
|-------|----------|-------|
| **File Naming** | `004_styleguide.md` → "File Organization" | `.App.` file naming, project prefixes |
| Variable Naming | `004_styleguide.md` → "Naming Conventions" | Fields, parameters, methods |
| Partial Files | `004_styleguide.md` → "Multi-Level Partial File Naming" | Splitting large files |

---

*Category: Style Guides*
*Last Updated: 2025-12-24*
