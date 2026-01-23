# 007 Patterns Index

> Category index for reusable FreeCRM patterns and utilities.

**Source:** FreeCRM base template (public)

---

## Quick Navigation

| Document | Description | When to Use |
|----------|-------------|-------------|
| [007_patterns.helpers.md](007_patterns.helpers.md) | Helpers static class reference | Using utility methods |
| [007_patterns.signalr.md](007_patterns.signalr.md) | Real-time updates with SignalR | Implementing live updates |

---

## Overview

Pattern guides document reusable code patterns that appear across multiple FreeCRM projects. These are the "how to do X" guides for common tasks.

---

## Available Guides

### 007_patterns.helpers.md - Helpers & Utilities

**Purpose:** Complete reference for the `Helpers` static class.

**Key Topics:**
- Top 25 most-used helpers with examples
- Navigation: `NavigateTo()`, `BuildUrl()`, `ValidateUrl()`
- HTTP: `GetOrPost<T>()` for API calls
- Validation: `MissingValue()`, `MissingRequiredField()`
- Localization: `Text()`, `<Language>` component
- Serialization: `SerializeObject()`, `DeserializeObject<T>()`
- Extending with `Helpers.App.cs`

**Use when:** You need a utility function - check here first before writing your own.

---

### 007_patterns.signalr.md - SignalR Real-Time Updates

**Purpose:** Implement real-time data updates across clients.

**Key Topics:**
- Hub setup and tenant group management
- SignalRUpdate data structure and types
- Server-side broadcasting from DataAccess
- Client-side subscription in MainLayout
- Page-level handlers and cleanup

**Use when:** Building features that need instant updates when data changes.

---

## Pattern Categories

### Base Patterns (in all projects)
- Helpers utilities
- SignalR real-time updates
- Plugin system (future guide)
- Background processing (future guide)

### Advanced Patterns (project-specific)
- Workflow automation (see 008_components)
- Digital signatures (see 008_components)
- Network visualization (see 008_components)

---

## Future Additions

Planned pattern guides:
- `007_patterns.plugins.md` - Plugin architecture
- `007_patterns.workflow.md` - Workflow automation engine
- `007_patterns.background.md` - Background task processing

---

*Category: Patterns*
*Last Updated: 2025-12-23*
