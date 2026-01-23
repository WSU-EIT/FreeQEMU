# 006 Architecture Index

> Category index for FreeCRM architecture and design documentation.

**Source:** FreeCRM base template (public). Examples from private repos nForm, Helpdesk4, DependencyManager, TrusselBuilder.

---

## FreeCRM Ecosystem Context

This documentation describes FreeCRM patterns. FreeCRM-main is the authoritative public base template. FreeManager and FreeCICD are public example extensions demonstrating how to build on FreeCRM.

---

## Quick Navigation

| Document | Description | When to Use |
|----------|-------------|-------------|
| [006_architecture.freecrm_overview.md](006_architecture.freecrm_overview.md) | .NET vs FreeCRM custom patterns | Understanding what's custom vs standard |
| [006_architecture.unique_features.md](006_architecture.unique_features.md) | Project-specific features analysis | Finding reusable patterns |

---

## Overview

Architecture guides explain the high-level design decisions in FreeCRM, clarify what's custom vs standard .NET, and document the overall structure of the framework.

---

## Available Guides

### 006_architecture.freecrm_overview.md - .NET vs FreeCRM

**Purpose:** Clarifies the boundary between standard .NET/Blazor and FreeCRM custom patterns.

**Key Topics:**
- DataObjects/DataAccess namespace patterns
- Navigation, authentication, HTTP helpers
- BlazorDataModel state management
- Multi-tenant vs single-tenant architecture
- Common gotchas for .NET developers

**Use when:** New to FreeCRM and wondering "is this .NET or custom?", or when deciding whether to use a helper vs standard .NET.

---

### 006_architecture.unique_features.md - Unique Features Analysis

**Purpose:** Catalogs features that exist in only 1-2 projects and are candidates for documentation or reuse.

**Key Topics:**
- Digital signature capture (nForm)
- Workflow automation engine (nForm)
- Network graph visualization (DependencyManager)
- SignalR real-time updates (base feature)
- Plugin system (base feature)

**Use when:** Looking for advanced patterns to implement, or deciding what features to port to other projects.

---

## Architecture Principles

FreeCRM follows these core architectural principles:

1. **Wrapper Pattern** - Many .NET features are wrapped with tenant-aware helpers
2. **Partial Classes** - Large classes split across files with `DataObjects.X.cs` pattern
3. **Autocomplete-Friendly** - Nested classes provide filtered IntelliSense
4. **Base Template + Extensions** - Core in FreeCRM-main, app-specific in `.App.cs` files

---

*Category: Architecture*
*Last Updated: 2025-12-23*
