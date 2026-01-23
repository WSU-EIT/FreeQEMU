# FreeCRM: Unique Features Analysis

> Catalog of unique features across FreeCRM-based projects for documentation and reuse opportunities.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Summary of Findings](#summary-of-findings) | ~30 | Feature overview tables |
| [Digital Signature Capture](#1-digital-signature-capture) | ~65 | jSignature pattern |
| [Workflow Automation](#2-workflow-automation-engine) | ~95 | Workflow types and execution |
| [CI/CD Pipeline Dashboard](#3-cicd-pipeline-dashboard) | ~130 | Pipeline monitoring pattern |
| [SignalR Real-Time Updates](#4-signalr-real-time-updates) | ~165 | Base feature overview |
| [Plugin System](#5-plugin-system) | ~205 | Extensibility architecture |
| [Documentation Priority](#recommended-documentation-priority) | ~240 | What to document next |

---

## Overview

**Purpose:** Identify unique features that exist in only 1-2 projects and are candidates for documentation or reuse in other projects.

**Analysis Scope:** 25+ FreeCRM-based repositories examined for unique patterns.

---

## Summary of Findings

### Unique Features (Project-Specific)

These features exist in only one or two projects:

| Feature | Source Project | Description | Doc Priority |
|---------|----------------|-------------|--------------|
| Digital Signature Capture | nForm (private) | jSignature-based signature pad | HIGH |
| Workflow Automation Engine | nForm (private) | C# code execution, email workflows | HIGH |
| Network Graph Visualization | DependencyManager (private) | vis.js network charts | DONE |
| CI/CD Pipeline Dashboard | FreeCICD (public example) | Azure DevOps monitoring | MEDIUM |
| IP Address Manager | Helpdesk4 (private) | Network/firewall tracking | LOW |
| SSO/TOTP Integration | SSO (private) | Okta MFA enrollment | MEDIUM |

### Base Features (Common Across Projects)

These features are part of the FreeCRM base template:

| Feature | Documentation Status | Notes |
|---------|---------------------|-------|
| SignalR Real-Time Updates | **DOCUMENTED** (007_patterns.signalr.md) | Used in ALL projects |
| Plugin System | Needs documentation | Complex extensibility |
| BackgroundProcessor | Standard pattern | Background tasks |
| PDF Viewer | Standard usage | PSPDFKit wrapper |
| Highcharts | Standard usage | Charting library |
| Monaco Editor | **DOCUMENTED** (008_components.monaco.md) | Code editing |

---

## Detailed Feature Analysis

### 1. Digital Signature Capture

**Source:** Private repo "nForm"
**Documentation:** See [008_components.signature.md](008_components.signature.md)

**Technology Stack:**
- jSignature JavaScript library for touch-friendly capture
- DotNetObjectReference for JavaScript→C# callbacks
- Colocated `.razor.js` file pattern
- Two-way binding with `@bind-Value`

**Key Pattern Preview:**

The signature component demonstrates the DotNetObjectReference callback pattern, which enables JavaScript to call back into C# methods:

```csharp
// Create a reference that JavaScript can use to call back to this component
dotNetHelper = DotNetObjectReference.Create(this);
await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);

// This method can be called from JavaScript
[JSInvokable]
public void SignatureUpdated(string value)
{
    Value = value;
    ValueChanged.InvokeAsync(Value);
}
```

This pattern is reusable for any component needing JS→C# communication.

---

### 2. Workflow Automation Engine

**Source:** Private repo "nForm"
**Documentation:** Recommended for future guide

**Workflow Types Supported:**
- `csharp` - Dynamic C# code execution with Monaco editor
- `email` - Email notifications with attachments
- `emailapproval` - Multi-step approval workflows with retries
- `filter` - Content filtering with search words
- `customvariable` - Dynamic variable injection
- `delete` - Record deletion workflows
- `externalapp` - External application execution
- `plugin:*` - Plugin-based workflow extensions

**Key Capabilities:**
- Monaco editor for C# code with syntax highlighting
- Email composition with HTML editor
- Form attachment as PDF/HTML
- Retry logic with configurable intervals
- Plugin prompt integration

**Recommendation:** Create dedicated guide documenting workflow architecture and how to extend it.

---

### 3. CI/CD Pipeline Dashboard

**Source:** Public example extension "FreeCICD" (community-contributed)
**Documentation:** Lower priority - specialized use case

**Features:**
- Azure DevOps pipeline monitoring
- Recursive folder hierarchy organization
- Card and Table view modes
- Multi-filter system (status, result, repository, trigger)
- Real-time updates via SignalR

**Unique Pattern - Recursive Folder Nodes:**

This pattern demonstrates how to organize hierarchical data with recursive traversal:

```csharp
// A tree structure for organizing items in folders
public class FolderNode
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public List<FolderNode> SubFolders { get; set; } = [];
    public List<DataObjects.ItemListItem> Items { get; set; } = [];

    // Recursively get all items from this folder and all subfolders
    public List<DataObjects.ItemListItem> GetAllItems()
    {
        var all = new List<DataObjects.ItemListItem>(Items);
        foreach (var sub in SubFolders) {
            all.AddRange(sub.GetAllItems());
        }
        return all;
    }
}
```

This pattern is useful for any feature needing folder/category hierarchies.

---

### 4. SignalR Real-Time Updates

**Source:** FreeCRM base template (all projects)
**Documentation:** See [007_patterns.signalr.md](007_patterns.signalr.md)

SignalR is the backbone of real-time updates in FreeCRM applications. Key aspects:

**Core Components:**
- `signalrHub.cs` - Server-side hub management
- `DataObjects.SignalR.cs` - Update type definitions
- `DataAccess.SignalR.cs` - Server-side broadcasting
- `MainLayout.razor` - Client connection setup

**Standard Page Handler Pattern:**

Every page that needs real-time updates follows this subscription pattern:

```csharp
// Subscribe in OnInitialized
Model.OnSignalRUpdate += SignalRUpdate;

// Handle updates relevant to this page
protected async void SignalRUpdate(DataObjects.SignalRUpdate update)
{
    // Only react to updates from OTHER users, not yourself
    if (update.UpdateType == DataObjects.SignalRUpdateType.YourType
        && update.UserId != Model.User.UserId)
    {
        await LoadData();
    }
}

// Always clean up in Dispose
public void Dispose()
{
    Model.OnSignalRUpdate -= SignalRUpdate;
}
```

This pattern ensures pages stay in sync without manual refresh.

---

### 5. Plugin System

**Source:** FreeCRM base template (all projects)
**Documentation:** Recommended for future guide

The plugin system provides extensibility for:
- Workflow plugins
- User authentication plugins
- Data transformation plugins
- Background processing plugins

**Core Components:**
- `Plugins.cs` - Plugin execution engine
- `DataAccess.Plugins.cs` - Plugin data access
- `DataController.Plugins.cs` - Plugin API endpoints

**Key Execution Pattern:**

Plugins are executed by passing objects and receiving results:

```csharp
// Execute a plugin with parameters
var result = await Helpers.ExecutePlugin(plugin, [form, formData, processing]);

// Handle the result - plugins return objects that need deserialization
if (result.Objects != null && result.Objects.Count > 0) {
    var processedData = Helpers.DeserializeJsonDocumentObject<DataObjects.FormData>(
        result.Objects[0]
    );
}
```

**Recommendation:** Create dedicated guide documenting plugin architecture, registration, and best practices.

---

## Recommended Documentation Priority

### Phase 1: High Priority (Immediately Useful)

1. **SignalR Guide** - DONE (007_patterns.signalr.md)
2. **Signature Capture Guide** - DONE (008_components.signature.md)
3. **Network Chart Guide** - DONE (008_components.network_chart.md)

### Phase 2: Medium Priority (Advanced Features)

4. **007_patterns.workflow.md** - Workflow automation
5. **007_patterns.plugins.md** - Plugin architecture

### Phase 3: Lower Priority (Specialized)

6. CI/CD dashboard patterns
7. Asset management patterns
8. SSO/MFA integration patterns

---

## Base Components (Standard Usage)

These components don't need dedicated guides as they follow standard patterns:

- **PDF Viewer** - PSPDFKit wrapper, standard configuration
- **Highcharts** - Chart library wrapper, standard options API
- **HtmlEditorDialog** - Rich text editor, standard TinyMCE/Quill patterns
- **UserDefinedFields** - Dynamic fields, similar to UDF patterns elsewhere
- **BackgroundProcessor** - Standard IHostedService pattern

---

*Category: 006_architecture*
*Last Updated: 2025-12-23*
*Based on: Analysis of 25+ FreeCRM-based repositories*
