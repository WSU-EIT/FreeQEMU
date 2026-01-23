# 008 Components Index

> Category index for FreeCRM UI component documentation.

**Source:** FreeCRM base template (public), examples from various private repos

---

## Quick Navigation

| Document | Description | When to Use |
|----------|-------------|-------------|
| [008_components.razor_templates.md](008_components.razor_templates.md) | Page structure templates | Creating new pages |
| [008_components.highcharts.md](008_components.highcharts.md) | Charts and reporting | Building dashboards |
| [008_components.wizard.md](008_components.wizard.md) | Multi-step wizard UI | Setup flows, onboarding |
| [008_components.monaco.md](008_components.monaco.md) | Monaco code editor integration | Adding code editing |
| [008_components.network_chart.md](008_components.network_chart.md) | vis.js graph visualization | Building node/edge diagrams |
| [008_components.signature.md](008_components.signature.md) | Digital signature capture | Capturing signatures |

---

## Overview

Component guides document specific UI components, their implementation patterns, and how to integrate them into your application.

---

## Available Guides

### 008_components.razor_templates.md - Razor Page Templates

**Purpose:** Standard templates for building pages that follow FreeCRM conventions.

**Key Topics:**
- List page template with filtering
- Edit page template with save/delete
- Multi-tenant vs single-tenant routing
- Standard page lifecycle patterns
- Permission checks and validation

**Use when:** Creating new pages - copy these templates as starting points.

---

### 008_components.highcharts.md - Highcharts Reporting

**Purpose:** Build interactive charts and reporting dashboards.

**Key Topics:**
- Column and Pie chart types
- Click handlers for drill-down
- Dynamic CDN loading (no npm)
- SeriesData and SeriesDataArray models
- Reporting page patterns

**Use when:** Building dashboards, reports, or any data visualization with charts.

**Source:** FreeCRM base template (public), private repo Helpdesk4 reporting pages

---

### 008_components.wizard.md - Multi-Step Wizard

**Purpose:** Build step-by-step wizard interfaces with visual progress.

**Key Topics:**
- WizardStepper visual progress indicator
- WizardStepHeader navigation controls
- SelectionSummary for past choices
- Step state management
- Import/pre-fill patterns

**Use when:** Setup flows, onboarding, configuration wizards, or any multi-step process.

**Source:** Public example extension "FreeCICD" (community-contributed)

---

### 008_components.monaco.md - Monaco Code Editor

**Purpose:** Integrate the Monaco editor for code editing features.

**Key Topics:**
- BlazorMonaco component setup
- Language modes and syntax highlighting
- Read-only vs editable modes
- Getting/setting content programmatically
- Diff editor for comparisons

**Use when:** Building features that need code/text editing with syntax highlighting.

**Note:** Primarily used in private repo nForm for form building with C# expressions.

---

### 008_components.network_chart.md - Network Graph Visualization

**Purpose:** Build interactive node-and-edge graph visualizations.

**Key Topics:**
- vis.js Network integration
- Node and relationship data structures
- Physics solver configuration
- Click handlers via DotNetObjectReference
- Layout persistence

**Use when:** Visualizing relationships, dependencies, or hierarchical data.

**Source:** Pattern derived from private repo "DependencyManager"

---

### 008_components.signature.md - Digital Signature Capture

**Purpose:** Capture touch-friendly digital signatures.

**Key Topics:**
- jSignature library integration
- DotNetObjectReference callback pattern
- Two-way binding implementation
- Colocated .razor.js module pattern
- Signature data storage format

**Use when:** Building forms that require signature capture.

**Source:** Pattern derived from private repo "nForm"

---

## Component Categories

### Base Components (in most projects)
- **Highcharts** - Charting library **[DOCUMENTED]**
- PDF Viewer (PSPDFKit wrapper)
- HtmlEditorDialog (rich text editing)
- UserDefinedFields (dynamic fields)

### Advanced Components (project-specific)
- **Wizard** - Multi-step flows **[DOCUMENTED]**
- **Monaco Editor** - Code editing **[DOCUMENTED]**
- **NetworkChart** - Graph visualization **[DOCUMENTED]**
- **Signature** - Digital signatures **[DOCUMENTED]**
- Workflow - Automation engine (future guide)

---

## Common Patterns Across Components

All documented components follow these patterns:

1. **Colocated JS** - `.razor.js` files alongside `.razor` components
2. **DotNetObjectReference** - For JavaScript→C# callbacks
3. **IDisposable** - Proper cleanup of JS references
4. **Two-way Binding** - `@bind-Value` pattern for data

---

*Category: Components*
*Last Updated: 2025-12-23*
