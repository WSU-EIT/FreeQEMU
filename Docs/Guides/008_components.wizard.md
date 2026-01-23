# FreeCRM: Multi-Step Wizard Pattern Guide

> Patterns for building step-by-step wizard interfaces with visual progress in FreeCRM-based Blazor applications.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~30 | What the wizard pattern provides |
| [Component Architecture](#component-architecture) | ~55 | Files and structure |
| [WizardStepper Component](#wizardstepper-component) | ~80 | Visual progress indicator |
| [WizardStepHeader Component](#wizardstepheader-component) | ~180 | Step navigation controls |
| [SelectionSummary Component](#selectionsummary-component) | ~260 | Past selections display |
| [Orchestrating the Wizard](#orchestrating-the-wizard) | ~320 | Main wizard logic |
| [Complete Example](#complete-example) | ~450 | Full wizard implementation |
| [Best Practices](#best-practices) | ~600 | Tips and recommendations |

**Source:** Public example extension "FreeCICD" (community-contributed, demonstrates extending FreeCRM)
**Also used in:** EntityWizard (FreeCRM template generator)

> **Note:** FreeCICD is a public example of extending FreeCRM. The patterns shown here are functional but not written by the core FreeCRM team - use as a reference for how to implement wizards, not as authoritative style.

---

## Overview

The Wizard pattern provides:
- Visual step progress indicator (numbered circles with connecting lines)
- Navigation to previous completed steps
- Selection summary showing past choices
- Step headers with Next/Back buttons
- State management for multi-step flows

**When to use:**
- Multi-step configuration flows
- Setup wizards (initial app configuration)
- Complex form submissions with multiple stages
- Import/migration workflows

**Components:**
- `WizardStepper` - Horizontal step indicator with progress
- `WizardStepHeader` - Card header with navigation buttons
- `SelectionSummary` - Shows previous selections
- Individual step components - Content for each step

---

## Component Architecture

### File Structure

```
YourProject.Client/
└── Shared/
    └── Wizard/
        ├── WizardStepper.App.YourApp.razor
        ├── WizardStepHeader.App.YourApp.razor
        ├── SelectionSummary.App.YourApp.razor
        ├── WizardStepOne.App.YourApp.razor
        ├── WizardStepTwo.App.YourApp.razor
        └── WizardStepCompleted.App.YourApp.razor
```

The `.App.YourApp.razor` naming follows FreeCRM conventions for app-specific extensions.

---

## WizardStepper Component

The WizardStepper displays numbered circles with connecting lines showing progress through the wizard:

### WizardStepper.razor

```razor
@* Numbered step circles with visual progress using Bootstrap *@

<div class="d-flex justify-content-between align-items-start bg-light rounded-3 p-3 mb-3 shadow-sm overflow-auto">
    @for (int i = 0; i < Steps.Count; i++) {
        var stepIndex = i;
        var step = Steps[i];
        var isCompleted = stepIndex < CurrentStep;
        var isCurrent = stepIndex == CurrentStep;
        var isClickable = isCompleted && OnStepClick.HasDelegate;

        <div class="d-flex flex-column align-items-center flex-fill position-relative" style="min-width: 70px;">
            @* Step Circle *@
            <div class="rounded-circle d-flex align-items-center justify-content-center fw-bold border border-2
                        @(isCompleted ? "bg-success border-success text-white" :
                          isCurrent ? "bg-primary border-primary text-white shadow" :
                          "bg-white border-secondary text-muted")
                        @(isClickable ? "cursor-pointer" : "")"
                 style="width: 36px; height: 36px; z-index: 2; @(isClickable ? "cursor: pointer;" : "")"
                 @onclick="@(() => HandleStepClick(stepIndex))"
                 title="@(isClickable ? $"Go to {step.Name}" : step.Name)">
                @if (isCompleted) {
                    <i class="fa fa-check"></i>
                } else if (isCurrent) {
                    <i class="fa fa-circle fa-xs"></i>
                } else {
                    <span class="small">@(stepIndex + 1)</span>
                }
            </div>

            @* Connector Line - connects to next step *@
            @if (stepIndex < Steps.Count - 1) {
                <div class="position-absolute @(isCompleted ? "bg-success" : "bg-secondary")"
                     style="height: 3px; top: 18px; left: 50%; width: 100%; z-index: 1; opacity: 0.5;"></div>
            }

            @* Step Label *@
            <div class="text-center mt-2" style="max-width: 80px;">
                <div class="small fw-semibold @(isCompleted ? "text-success" : isCurrent ? "text-primary" : "text-muted") text-truncate">
                    @step.ShortName
                </div>
                @* Show selected value for completed/current steps *@
                @if (!string.IsNullOrWhiteSpace(step.SelectedValue) && (isCompleted || isCurrent)) {
                    <div class="text-muted small text-truncate" style="font-size: 0.7rem;" title="@step.SelectedValue">
                        @TruncateText(step.SelectedValue, 10)
                    </div>
                } else if (isCurrent) {
                    <div class="text-primary small fst-italic" style="font-size: 0.7rem;">(current)</div>
                }
            </div>
        </div>
    }
</div>

@code {
    // List of steps to display
    [Parameter] public List<WizardStepInfo> Steps { get; set; } = new();

    // Current step index (0-based)
    [Parameter] public int CurrentStep { get; set; }

    // Callback when user clicks a completed step
    [Parameter] public EventCallback<int> OnStepClick { get; set; }

    // Step information model
    public class WizardStepInfo
    {
        public string Name { get; set; } = "";       // Full name for tooltip
        public string ShortName { get; set; } = "";  // Short name for display
        public string SelectedValue { get; set; } = "";  // User's selection
    }

    private async Task HandleStepClick(int stepIndex)
    {
        // Only allow clicking on completed steps (going back)
        if (stepIndex < CurrentStep && OnStepClick.HasDelegate) {
            await OnStepClick.InvokeAsync(stepIndex);
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 2) + "..";
    }
}
```

The stepper visually shows: completed steps (green checkmark), current step (blue dot), and future steps (gray number).

---

## WizardStepHeader Component

Each step has a header card with title and navigation buttons:

### WizardStepHeader.razor

```razor
@* Step header with navigation buttons *@

<div class="card-header bg-white border-0 py-3">
    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
        <div class="d-flex align-items-center">
            @* Step number badge *@
            <span class="badge bg-primary rounded-pill me-2 fs-6">@StepNumber</span>
            <h5 class="mb-0">@Title</h5>
        </div>

        <div class="d-flex gap-2">
            @* Back button - only shown after first step *@
            @if (ShowBack) {
                <button type="button"
                        class="btn btn-outline-secondary"
                        @onclick="OnBack"
                        disabled="@IsDisabled">
                    <i class="fa fa-arrow-left me-1"></i> Back
                </button>
            }

            @* Next button - shown on most steps *@
            @if (ShowNext) {
                <button type="button"
                        class="btn btn-primary"
                        @onclick="OnNext"
                        disabled="@(IsDisabled || NextDisabled)">
                    @NextButtonText <i class="fa fa-arrow-right ms-1"></i>
                </button>
            }

            @* Finish button - shown on final step *@
            @if (ShowFinish) {
                <button type="button"
                        class="btn btn-success"
                        @onclick="OnFinish"
                        disabled="@(IsDisabled || FinishDisabled)">
                    <i class="fa fa-check me-1"></i> @FinishButtonText
                </button>
            }
        </div>
    </div>

    @* Optional subtitle/description *@
    @if (!string.IsNullOrWhiteSpace(Subtitle)) {
        <p class="text-muted mb-0 mt-2">@Subtitle</p>
    }
</div>

@code {
    [Parameter] public int StepNumber { get; set; }
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string? Subtitle { get; set; }

    [Parameter] public bool ShowBack { get; set; } = false;
    [Parameter] public bool ShowNext { get; set; } = false;
    [Parameter] public bool ShowFinish { get; set; } = false;

    [Parameter] public bool IsDisabled { get; set; } = false;
    [Parameter] public bool NextDisabled { get; set; } = false;
    [Parameter] public bool FinishDisabled { get; set; } = false;

    [Parameter] public string NextButtonText { get; set; } = "Next";
    [Parameter] public string FinishButtonText { get; set; } = "Finish";

    [Parameter] public EventCallback OnBack { get; set; }
    [Parameter] public EventCallback OnNext { get; set; }
    [Parameter] public EventCallback OnFinish { get; set; }
}
```

The header adapts based on which buttons should be shown for the current step.

---

## SelectionSummary Component

Shows a compact summary of previous selections:

### SelectionSummary.razor

```razor
@* Compact display of wizard selections made so far *@

@if (Selections.Any()) {
    <div class="bg-light rounded-3 p-2 mb-3 d-flex flex-wrap gap-2 align-items-center">
        <small class="text-muted me-2">
            <i class="fa fa-info-circle"></i> Selected:
        </small>
        @foreach (var selection in Selections) {
            <span class="badge bg-secondary">
                @selection.Label: <strong>@selection.Value</strong>
            </span>
        }
    </div>
}

@code {
    [Parameter] public List<SelectionItem> Selections { get; set; } = new();

    public class SelectionItem
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
```

This provides context as users progress through the wizard.

---

## Orchestrating the Wizard

The main wizard page orchestrates all components and manages state:

### Core State Management

```csharp
@code {
    // Current step (0-based index)
    protected int currentStep = 0;

    // Step names enum for type-safe step identification
    public enum StepNames { SelectProject, SelectBranch, Configure, Review, Complete }

    // Map step index to step name
    protected StepNames[] StepOrder = {
        StepNames.SelectProject,
        StepNames.SelectBranch,
        StepNames.Configure,
        StepNames.Review,
        StepNames.Complete
    };

    // Current step name
    protected StepNames CurrentStepName => StepOrder[currentStep];

    // User selections
    protected string _selectedProject = "";
    protected string _selectedBranch = "";
    protected DataObjects.ConfigOptions _config = new();

    // Navigation methods
    protected async Task GoToNextStep()
    {
        if (currentStep < StepOrder.Length - 1) {
            currentStep++;
            StateHasChanged();
        }
    }

    protected async Task GoToPreviousStep()
    {
        if (currentStep > 0) {
            currentStep--;
            StateHasChanged();
        }
    }

    protected async Task GoToStep(int step)
    {
        // Only allow going back to completed steps
        if (step < currentStep) {
            currentStep = step;
            StateHasChanged();
        }
    }
}
```

### Building Step Info for WizardStepper

```csharp
protected List<WizardStepper.WizardStepInfo> GetWizardSteps()
{
    return new List<WizardStepper.WizardStepInfo> {
        new() {
            Name = "Select Project",
            ShortName = "Project",
            SelectedValue = _selectedProject
        },
        new() {
            Name = "Select Branch",
            ShortName = "Branch",
            SelectedValue = _selectedBranch
        },
        new() {
            Name = "Configure Options",
            ShortName = "Config",
            SelectedValue = _config.IsConfigured ? "Configured" : ""
        },
        new() {
            Name = "Review & Confirm",
            ShortName = "Review",
            SelectedValue = ""
        },
        new() {
            Name = "Complete",
            ShortName = "Done",
            SelectedValue = ""
        }
    };
}
```

### Building Selection Summary

```csharp
protected List<SelectionSummary.SelectionItem> GetSelectionSummary()
{
    var selections = new List<SelectionSummary.SelectionItem>();

    if (!string.IsNullOrWhiteSpace(_selectedProject)) {
        selections.Add(new() { Label = "Project", Value = _selectedProject });
    }

    if (!string.IsNullOrWhiteSpace(_selectedBranch)) {
        selections.Add(new() { Label = "Branch", Value = _selectedBranch });
    }

    return selections;
}
```

---

## Complete Example

Here's a complete wizard implementation:

```razor
@page "/Setup"
@page "/{TenantCode}/Setup"
@implements IDisposable

@if (Model.Loaded && Model.View == _pageName) {
    <h1 class="page-title">
        <Language Tag="SetupWizard" IncludeIcon="true" />
    </h1>

    <div class="container" style="max-width: 800px;">

        @* Step Progress Indicator *@
        <WizardStepper Steps="@GetWizardSteps()"
                       CurrentStep="@currentStep"
                       OnStepClick="@GoToStep" />

        @* Selection Summary *@
        @if (currentStep > 0) {
            <SelectionSummary Selections="@GetSelectionSummary()" />
        }

        @* Main Wizard Card *@
        <div class="card shadow-sm">

            @* Step Content - switches based on current step *@
            @switch (CurrentStepName) {

                case StepNames.SelectProject:
                    <WizardStepHeader StepNumber="1"
                                      Title="Select Project"
                                      Subtitle="Choose the project to configure"
                                      ShowNext="true"
                                      NextDisabled="@string.IsNullOrWhiteSpace(_selectedProject)"
                                      OnNext="GoToNextStep" />

                    <div class="card-body">
                        <div class="list-group">
                            @foreach (var project in _projects) {
                                <button type="button"
                                        class="list-group-item list-group-item-action @(_selectedProject == project.Name ? "active" : "")"
                                        @onclick="@(() => SelectProject(project))">
                                    <strong>@project.Name</strong>
                                    <br />
                                    <small class="text-muted">@project.Description</small>
                                </button>
                            }
                        </div>
                    </div>
                    break;

                case StepNames.SelectBranch:
                    <WizardStepHeader StepNumber="2"
                                      Title="Select Branch"
                                      ShowBack="true"
                                      ShowNext="true"
                                      NextDisabled="@string.IsNullOrWhiteSpace(_selectedBranch)"
                                      OnBack="GoToPreviousStep"
                                      OnNext="GoToNextStep" />

                    <div class="card-body">
                        @if (_loadingBranches) {
                            <LoadingMessage />
                        } else {
                            <select class="form-select" @bind="_selectedBranch">
                                <option value="">-- Select Branch --</option>
                                @foreach (var branch in _branches) {
                                    <option value="@branch">@branch</option>
                                }
                            </select>
                        }
                    </div>
                    break;

                case StepNames.Configure:
                    <WizardStepHeader StepNumber="3"
                                      Title="Configure Options"
                                      ShowBack="true"
                                      ShowNext="true"
                                      OnBack="GoToPreviousStep"
                                      OnNext="GoToNextStep" />

                    <div class="card-body">
                        <div class="mb-3">
                            <label class="form-label">Option 1</label>
                            <input type="text" class="form-control" @bind="_config.Option1" />
                        </div>
                        <div class="form-check mb-3">
                            <input type="checkbox" class="form-check-input" @bind="_config.EnableFeature" />
                            <label class="form-check-label">Enable Feature X</label>
                        </div>
                    </div>
                    break;

                case StepNames.Review:
                    <WizardStepHeader StepNumber="4"
                                      Title="Review & Confirm"
                                      ShowBack="true"
                                      ShowFinish="true"
                                      FinishButtonText="Create"
                                      FinishDisabled="@_isProcessing"
                                      OnBack="GoToPreviousStep"
                                      OnFinish="CompleteWizard" />

                    <div class="card-body">
                        <h6>Summary</h6>
                        <ul>
                            <li><strong>Project:</strong> @_selectedProject</li>
                            <li><strong>Branch:</strong> @_selectedBranch</li>
                            <li><strong>Option 1:</strong> @_config.Option1</li>
                            <li><strong>Feature X:</strong> @(_config.EnableFeature ? "Enabled" : "Disabled")</li>
                        </ul>

                        @if (_isProcessing) {
                            <div class="alert alert-info">
                                <i class="fa fa-spinner fa-spin me-2"></i> Processing...
                            </div>
                        }
                    </div>
                    break;

                case StepNames.Complete:
                    <WizardStepHeader StepNumber="5"
                                      Title="Complete!"
                                      Subtitle="Your configuration has been created" />

                    <div class="card-body text-center">
                        <i class="fa fa-check-circle text-success fa-5x mb-3"></i>
                        <h4>Setup Complete!</h4>
                        <p>Your project has been configured successfully.</p>
                        <a href="@Helpers.BuildUrl("Dashboard")" class="btn btn-primary">
                            Go to Dashboard
                        </a>
                    </div>
                    break;
            }
        </div>
    </div>
}

@code {
    protected string _pageName = "setup-wizard";
    protected int currentStep = 0;

    public enum StepNames { SelectProject, SelectBranch, Configure, Review, Complete }
    protected StepNames[] StepOrder = {
        StepNames.SelectProject, StepNames.SelectBranch,
        StepNames.Configure, StepNames.Review, StepNames.Complete
    };
    protected StepNames CurrentStepName => StepOrder[currentStep];

    // Selections
    protected string _selectedProject = "";
    protected string _selectedBranch = "";
    protected ConfigOptions _config = new();

    // State
    protected bool _loadingBranches = false;
    protected bool _isProcessing = false;
    protected List<Project> _projects = [];
    protected List<string> _branches = [];

    protected async Task SelectProject(Project project)
    {
        _selectedProject = project.Name;
        _loadingBranches = true;
        StateHasChanged();

        // Load branches for selected project
        _branches = await Helpers.GetOrPost<List<string>>($"Data/GetBranches/{project.Id}");
        _loadingBranches = false;
        StateHasChanged();
    }

    protected async Task CompleteWizard()
    {
        _isProcessing = true;
        StateHasChanged();

        // Save configuration
        var result = await Helpers.GetOrPost<bool>("Data/SaveConfig", new {
            Project = _selectedProject,
            Branch = _selectedBranch,
            Config = _config
        });

        _isProcessing = false;
        currentStep++;  // Move to Complete step
        StateHasChanged();
    }

    // Navigation
    protected void GoToNextStep() { if (currentStep < StepOrder.Length - 1) currentStep++; }
    protected void GoToPreviousStep() { if (currentStep > 0) currentStep--; }
    protected void GoToStep(int step) { if (step < currentStep) currentStep = step; }

    // Build step info for WizardStepper
    protected List<WizardStepper.WizardStepInfo> GetWizardSteps() { /* ... */ }
    protected List<SelectionSummary.SelectionItem> GetSelectionSummary() { /* ... */ }
}
```

---

## Best Practices

### 1. Use Enums for Step Names

```csharp
// CORRECT - type-safe step identification
public enum StepNames { Step1, Step2, Step3 }
protected StepNames CurrentStepName => StepOrder[currentStep];

// AVOID - magic numbers
if (currentStep == 2) { /* ... */ }
```

### 2. Validate Before Allowing Next

```razor
<WizardStepHeader ShowNext="true"
                  NextDisabled="@(!IsStepValid())"
                  OnNext="GoToNextStep" />
```

### 3. Show Loading States

```razor
@if (_loadingStepData) {
    <LoadingMessage />
} else {
    @* Step content *@
}
```

### 4. Allow Going Back

```csharp
protected void GoToStep(int step)
{
    // Only allow going back, not forward
    if (step < currentStep) {
        currentStep = step;
        StateHasChanged();
    }
}
```

### 5. Confirm Before Final Step

```razor
<WizardStepHeader ShowFinish="true"
                  FinishButtonText="Create"
                  OnFinish="CompleteWizard" />

@code {
    protected async Task CompleteWizard()
    {
        // Optional: Show confirmation
        if (!await Confirm("Are you sure?")) return;

        _isProcessing = true;
        // ... do work ...
    }
}
```

### 6. Handle Import/Pre-fill Scenarios

```csharp
// Check for import parameter on load
protected override void OnInitialized()
{
    var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
    if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("import", out var importId)) {
        await PreFillFromImport(importId);
    }
}
```

---

*Category: 008_components*
*Last Updated: 2025-12-23*
*Source: Public example extension "FreeCICD" (community-contributed)*
