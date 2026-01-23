# FreeCRM: Highcharts Reporting Guide

> Patterns for building interactive charts and reports using Highcharts in FreeCRM-based Blazor applications.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~30 | What Highcharts provides |
| [Component Architecture](#component-architecture) | ~50 | Files and structure |
| [Razor Component](#the-highcharts-component) | ~75 | C# implementation |
| [JavaScript Module](#javascript-module) | ~180 | Chart rendering and callbacks |
| [Data Structures](#data-structures) | ~280 | SeriesData and SeriesDataArray |
| [Usage Examples](#usage-examples) | ~320 | Pie, column, and click handlers |
| [Reporting Page Pattern](#reporting-page-pattern) | ~420 | Full report page example |
| [Best Practices](#best-practices) | ~550 | Tips and recommendations |

**Source:** FreeCRM base template
**Library:** [Highcharts](https://www.highcharts.com/) via CDN

---

## Overview

The Highcharts component provides:
- Column charts for comparing categories
- Pie charts for showing proportions
- Click handlers that drill down into data
- Dynamic CDN loading (no npm required)
- DotNetObjectReference callback pattern

**When to use:**
- Bar/line/pie charts for dashboards
- Reporting pages with data visualizations
- Interactive charts that drill down

**When NOT to use:**
- Node-and-edge graphs (use NetworkChart with vis.js)
- Code editing (use Monaco)

---

## Component Architecture

### Files Required

| File | Purpose |
|------|---------|
| `Highcharts.razor` | Blazor component with chart logic |
| `Highcharts.razor.js` | Colocated JavaScript for rendering |

### File Location

```
YourProject.Client/
└── Shared/
    ├── Highcharts.razor
    └── Highcharts.razor.js
```

No npm packages needed - libraries are loaded from CDN dynamically.

---

## The Highcharts Component

### Highcharts.razor

This component wraps the Highcharts library with a Blazor-friendly interface:

```csharp
@implements IDisposable
@inject IJSRuntime jsRuntime

@code {
    // === PARAMETERS ===

    // Title displayed at top of chart
    [Parameter] public string? ChartTitle { get; set; }

    // Subtitle displayed below title
    [Parameter] public string? ChartSubtitle { get; set; }

    // Type of chart to render
    [Parameter] public ChartTypes? ChartType { get; set; }

    // HTML element ID to render chart into
    [Parameter] public string? ElementId { get; set; }

    // Callback when user clicks a chart item
    [Parameter] public Delegate? OnItemClicked { get; set; }

    // Y-axis label (for column charts)
    [Parameter] public string? yAxisText { get; set; }

    // Category labels for x-axis (column charts)
    [Parameter] public string[]? SeriesCategories { get; set; }

    // Data for pie charts
    [Parameter] public SeriesData[]? SeriesDataItems { get; set; }

    // Data for column charts
    [Parameter] public SeriesDataArray[]? SeriesDataArrayItems { get; set; }

    // === INTERNAL STATE ===
    protected IJSObjectReference? jsModule;
    protected DotNetObjectReference<Highcharts>? dotNetHelper;
    protected bool _highchartsResourcesLoaded = false;

    // === LIFECYCLE ===
    public void Dispose()
    {
        dotNetHelper?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            // Create reference for JS callbacks
            dotNetHelper = DotNetObjectReference.Create(this);

            // Import colocated JavaScript module
            jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Shared/Highcharts.razor.js?v=" + Guid.NewGuid().ToString().Replace("-", ""));

            // Pass reference to JavaScript
            await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);

            // Load Highcharts libraries from CDN
            await jsModule.InvokeVoidAsync("LoadHighchartsResources");
        }
    }

    // Called from JavaScript when libraries are loaded
    [JSInvokable]
    public async Task OnHighchartsLoaded()
    {
        if (!_highchartsResourcesLoaded) {
            _highchartsResourcesLoaded = true;
            await RenderChart();
        }
    }

    // Called from JavaScript when user clicks chart item
    [JSInvokable]
    public void ChartItemClicked(int index)
    {
        if (OnItemClicked != null) {
            OnItemClicked.DynamicInvoke(index);
        }
    }

    // === CHART TYPES ===
    public enum ChartTypes
    {
        Column,
        Pie,
    }

    // === DATA STRUCTURES ===

    // For pie charts - each slice has name, value, and tooltip
    public class SeriesData
    {
        public string name { get; set; } = "";
        public decimal data { get; set; }
        public string tooltip { get; set; } = "";
    }

    // For column charts - each series has name and array of values
    public class SeriesDataArray
    {
        public string name { get; set; } = "";
        public decimal[] data { get; set; } = [];
    }
}
```

The component re-renders when parameters change, automatically updating the chart.

---

## JavaScript Module

### Highcharts.razor.js

This JavaScript module handles library loading and chart rendering:

```javascript
var dotNetHelper;

// Store reference for callbacks to C#
export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

// Load Highcharts libraries from CDN if not already loaded
export function LoadHighchartsResources() {
    if (typeof(Highcharts) == "object") {
        // Already loaded - notify C#
        dotNetHelper.invokeMethodAsync("OnHighchartsLoaded");
    } else {
        // Chain-load libraries in order
        LoadCssResource("https://code.highcharts.com/css/highcharts.css", "highcharts-light", () => {
            LoadScriptResource("https://code.highcharts.com/highcharts.js", () => {
                LoadScriptResource("https://code.highcharts.com/modules/exporting.js", () => {
                    LoadScriptResource("https://code.highcharts.com/modules/export-data.js", () => {
                        LoadScriptResource("https://code.highcharts.com/modules/accessibility.js", () => {
                            dotNetHelper.invokeMethodAsync("OnHighchartsLoaded");
                        });
                    });
                });
            });
        });
    }
}

// Render a column chart
export function RenderChart_Column(elementId, chartTitle, chartSubtitle, yAxisText, seriesCategories, seriesData) {
    Highcharts.chart(elementId, {
        chart: { type: 'column', styledMode: true },
        credits: { enabled: false },
        title: { text: chartTitle },
        subtitle: { text: chartSubtitle, useHTML: true },
        xAxis: { categories: seriesCategories, crosshair: true },
        yAxis: { min: 0, title: { text: yAxisText } },
        legend: { enabled: false },
        series: seriesData,
        plotOptions: {
            series: {
                cursor: 'pointer',
                point: {
                    events: {
                        click: function (event) {
                            // Call back to C# with clicked index
                            dotNetHelper.invokeMethodAsync("ChartItemClicked", this.index);
                        }
                    }
                }
            }
        }
    });
}

// Render a pie chart
export function RenderChart_Pie(elementId, chartTitle, chartSubtitle, seriesData) {
    // Convert C# data to Highcharts format
    var chartData = [];
    for (var x = 0; x < seriesData.length; x++) {
        chartData.push([seriesData[x].name, seriesData[x].data]);
    }

    Highcharts.chart(elementId, {
        chart: { type: 'pie', styledMode: true },
        credits: { enabled: false },
        title: { text: chartTitle },
        tooltip: {
            formatter: function () {
                return seriesData[this.point.index].tooltip;
            },
            useHTML: true
        },
        plotOptions: {
            pie: {
                allowPointSelect: true,
                cursor: 'pointer',
                dataLabels: { enabled: true, format: '<b>{point.name}</b>' }
            }
        },
        series: [{
            colorByPoint: true,
            data: chartData,
            point: {
                events: {
                    click: function (event) {
                        dotNetHelper.invokeMethodAsync("ChartItemClicked", this.index);
                    }
                }
            }
        }]
    });
}
```

The JavaScript dynamically loads Highcharts from CDN, so no npm install is required.

---

## Data Structures

### SeriesData (Pie Charts)

Each slice of the pie has a name, value, and custom tooltip:

```csharp
var pieData = new Highcharts.SeriesData[] {
    new() { name = "Engineering", data = 45, tooltip = "Engineering: 45 tickets (30%)" },
    new() { name = "Marketing", data = 30, tooltip = "Marketing: 30 tickets (20%)" },
    new() { name = "Sales", data = 75, tooltip = "Sales: 75 tickets (50%)" }
};
```

### SeriesDataArray (Column Charts)

Each series has a name and array of values matching the categories:

```csharp
// Categories are the x-axis labels
var categories = new string[] { "Jan", "Feb", "Mar", "Apr" };

// Each series is a colored bar group
var columnData = new Highcharts.SeriesDataArray[] {
    new() { name = "2024", data = new decimal[] { 100, 120, 90, 150 } },
    new() { name = "2025", data = new decimal[] { 110, 130, 100, 170 } }
};
```

---

## Usage Examples

### Basic Pie Chart

```razor
<div id="department-chart"></div>

<Highcharts ChartType="Highcharts.ChartTypes.Pie"
            ElementId="department-chart"
            ChartTitle="Tickets by Department"
            SeriesDataItems="_departmentData" />

@code {
    private Highcharts.SeriesData[] _departmentData = new[] {
        new Highcharts.SeriesData { name = "IT", data = 45, tooltip = "IT: 45" },
        new Highcharts.SeriesData { name = "HR", data = 30, tooltip = "HR: 30" },
        new Highcharts.SeriesData { name = "Finance", data = 25, tooltip = "Finance: 25" }
    };
}
```

### Column Chart with Categories

```razor
<div id="monthly-chart"></div>

<Highcharts ChartType="Highcharts.ChartTypes.Column"
            ElementId="monthly-chart"
            ChartTitle="Monthly Ticket Counts"
            yAxisText="Number of Tickets"
            SeriesCategories="_months"
            SeriesDataArrayItems="_monthlyData" />

@code {
    private string[] _months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };

    private Highcharts.SeriesDataArray[] _monthlyData = new[] {
        new Highcharts.SeriesDataArray {
            name = "Tickets",
            data = new decimal[] { 45, 52, 38, 67, 55, 48 }
        }
    };
}
```

### Chart with Click Handler

```razor
<div id="clickable-chart"></div>

<Highcharts ChartType="Highcharts.ChartTypes.Pie"
            ElementId="clickable-chart"
            ChartTitle="Click a slice to see details"
            SeriesDataItems="_departments"
            OnItemClicked="HandleChartClick" />

@code {
    private DataObjects.Department[] _departmentList = [];
    private Highcharts.SeriesData[] _departments = [];

    // Called when user clicks a pie slice
    private void HandleChartClick(int index)
    {
        // index corresponds to the clicked item's position in the array
        var clickedDepartment = _departmentList[index];

        // Navigate to department details or show modal
        Helpers.NavigateTo($"Reports/Department/{clickedDepartment.DepartmentId}");
    }
}
```

---

## Reporting Page Pattern

Here's how a complete reporting page with multiple charts can be structured (pattern from private repo Helpdesk4):

```razor
@page "/Reports/Tickets"
@page "/{TenantCode}/Reports/Tickets"
@implements IDisposable

@if (Model.Loaded && Model.View == _pageName) {
    <h1 class="page-title">
        <Language Tag="ReportTickets" IncludeIcon="true" />
    </h1>

    @if (_loading) {
        <LoadingMessage />
    } else {
        @* Chart Row - Two charts side by side *@
        <div class="row mb-4">
            <div class="col-md-6">
                <div id="tickets-by-campus"></div>

                @if (_ticketsByCampusData.Any()) {
                    <Highcharts ChartType="Highcharts.ChartTypes.Column"
                                ElementId="tickets-by-campus"
                                ChartTitle="@(Helpers.Text("ReportByCampus"))"
                                yAxisText="Ticket Count"
                                SeriesCategories="_campusCategories"
                                SeriesDataArrayItems="_ticketsByCampusData"
                                OnItemClicked="HandleCampusClick" />
                }
            </div>

            <div class="col-md-6">
                <div id="tickets-by-department"></div>

                @if (_ticketsByDepartment.Any()) {
                    <Highcharts ChartType="Highcharts.ChartTypes.Pie"
                                ElementId="tickets-by-department"
                                ChartTitle="@(Helpers.Text("ReportByDepartment"))"
                                SeriesDataItems="_ticketsByDepartment"
                                OnItemClicked="HandleDepartmentClick" />
                }
            </div>
        </div>

        @* Filter Controls *@
        <div class="mb-3">
            <select class="form-select w-auto d-inline-block" @onchange="ReportIntervalChanged">
                @foreach (var interval in Helpers.ReportIntervals) {
                    <option value="@interval.Value" selected="@(_selectedInterval == interval.Value)">
                        @interval.Id
                    </option>
                }
            </select>
        </div>
    }
}

@code {
    protected string _pageName = "report-tickets";
    protected bool _loading = true;

    // Chart data
    protected string[] _campusCategories = [];
    protected Highcharts.SeriesDataArray[] _ticketsByCampusData = [];
    protected Highcharts.SeriesData[] _ticketsByDepartment = [];

    // Raw data for drill-down
    protected List<DataObjects.Request> _tickets = [];
    protected List<DataObjects.Department> _departments = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Model.Loaded && Model.LoggedIn) {
            await LoadReportData();
            _loading = false;
            StateHasChanged();
        }
    }

    protected async Task LoadReportData()
    {
        // Load raw data
        _tickets = await Helpers.GetOrPost<List<DataObjects.Request>>("Data/GetTickets", _filter);
        _departments = Model.Departments;

        // Transform for charts
        BuildCampusChart();
        BuildDepartmentChart();
    }

    protected void BuildDepartmentChart()
    {
        _ticketsByDepartment = _departments
            .Select(d => new Highcharts.SeriesData {
                name = d.DepartmentName,
                data = _tickets.Count(t => t.DepartmentId == d.DepartmentId),
                tooltip = $"{d.DepartmentName}: {_tickets.Count(t => t.DepartmentId == d.DepartmentId)} tickets"
            })
            .Where(x => x.data > 0)
            .ToArray();
    }

    protected void HandleDepartmentClick(int index)
    {
        // Show tickets for clicked department
        var dept = _departments[index];
        _selectedDepartment = dept;
        _showDrillDown = true;
        StateHasChanged();
    }
}
```

This pattern demonstrates: filtering, multiple charts, drill-down on click, and responsive layout.

---

## Best Practices

### 1. Always Provide Element IDs

```razor
@* CORRECT - unique element ID *@
<div id="my-unique-chart"></div>
<Highcharts ElementId="my-unique-chart" ... />

@* WRONG - missing element ID *@
<Highcharts ... />
```

### 2. Check for Empty Data

```razor
@if (_chartData.Any()) {
    <Highcharts ... SeriesDataItems="_chartData" />
} else {
    <div class="text-muted">No data available</div>
}
```

### 3. Keep Raw Data for Drill-Down

```csharp
// Store both raw data and chart data
protected List<DataObjects.Department> _departments = [];  // For drill-down
protected Highcharts.SeriesData[] _departmentChartData = [];  // For chart

protected void HandleClick(int index)
{
    var clicked = _departments[index];  // Use raw data
}
```

### 4. Update Charts on Filter Change

```csharp
protected async Task FilterChanged()
{
    await LoadReportData();  // Reload data
    BuildChartData();        // Rebuild chart arrays
    StateHasChanged();       // Trigger re-render - chart updates automatically
}
```

### 5. Use Helpers.ReportIntervals for Date Ranges

```razor
<select @onchange="IntervalChanged">
    @foreach (var interval in Helpers.ReportIntervals) {
        <option value="@interval.Value">@interval.Id</option>
    }
</select>
```

---

## Adding New Chart Types

To extend Highcharts.razor with additional chart types:

1. Add to the `ChartTypes` enum:
```csharp
public enum ChartTypes { Column, Pie, Line, Bar }
```

2. Add rendering method in C#:
```csharp
protected async Task ChartRender_Line() { ... }
```

3. Add JavaScript render function:
```javascript
export function RenderChart_Line(elementId, ...) { ... }
```

4. Update the switch statement in `RenderChart()`.

---

*Category: 008_components*
*Last Updated: 2025-12-23*
*Source: FreeCRM base template (public), private repo Helpdesk4 reporting pages*
