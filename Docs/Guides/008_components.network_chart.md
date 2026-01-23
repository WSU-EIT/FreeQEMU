# FreeCRM: Network Chart Visualization Guide

> Patterns for building interactive network/graph visualizations using vis.js in FreeCRM-based Blazor applications.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~35 | What this component provides |
| [Component Structure](#part-1-component-structure) | ~55 | File organization |
| [Razor Component](#part-2-the-razor-component) | ~70 | C# implementation |
| [JavaScript Module](#part-3-the-javascript-module) | ~200 | vis.js integration |
| [Data Structures](#part-4-data-structures) | ~350 | Node and relationship models |
| [Physics Solvers](#part-5-physics-solvers) | ~420 | Layout algorithms |
| [Click Handlers](#part-6-click-handlers) | ~470 | Interactivity callbacks |
| [Complete Example](#part-7-putting-it-all-together) | ~520 | Full usage example |

**Source:** Private repo "DependencyManager"
**Library:** [vis.js Network](https://visjs.github.io/vis-network/docs/network/)

> **Note:** This is an advanced pattern for node-and-edge graph visualizations. For simpler charts (bar, line, pie), use Highcharts instead.

---

## Overview

The NetworkChart component provides:
- Interactive node-and-edge graph visualization
- Multiple physics simulation algorithms
- Bidirectional C#/JavaScript communication
- Click handlers for nodes and relationships
- Customizable node icons and colors
- Layout persistence (remembering arrangement)

**Library:** [vis.js Network](https://visjs.github.io/vis-network/docs/network/) via JavaScript interop

---

## Part 1: Component Structure

NetworkChart uses the **colocated .razor.js pattern** - a Razor component with a companion JavaScript file.

### File Structure

```
YourProject.Client/
└── Shared/
    ├── NetworkChart.razor      # Blazor component
    └── NetworkChart.razor.js   # JavaScript interop
```

---

## Part 2: The Razor Component

### NetworkChart.razor

```razor
@inject IJSRuntime jsRuntime

<style>
    #built-in-chart-element {
        background: #fff;
        width: 100%;
        height: calc(100vh - 200px);
        border: solid 1px #555;
    }
</style>

<div id="built-in-chart-element"></div>

@code {
    // === PARAMETERS ===
    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string? NetworkSolver { get; set; }
    [Parameter] public List<NetworkNode> Nodes { get; set; } = new List<NetworkNode>();
    [Parameter] public List<NetworkRelationship> Relationships { get; set; } = new List<NetworkRelationship>();
    [Parameter] public Delegate? OnElementSelected { get; set; }
    [Parameter] public Delegate? OnRelationshipSelected { get; set; }
    [Parameter] public string? Seed { get; set; }
    [Parameter] public string? StartNodeId { get; set; }

    // === INTERNAL STATE ===
    protected string _elementId = String.Empty;
    protected bool _firstRender = true;
    protected IJSObjectReference? jsModule;
    protected string _networkSolver = "";
    protected string _seed = "0";
    protected DotNetObjectReference<NetworkChart>? dotNetHelper;

    // === LIFECYCLE ===
    public void Dispose()
    {
        dotNetHelper?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            _elementId = !String.IsNullOrWhiteSpace(ElementId)
                ? ElementId : "built-in-chart-element";
            _networkSolver = !String.IsNullOrWhiteSpace(NetworkSolver)
                ? NetworkSolver : "repulsion";
            _seed = !String.IsNullOrWhiteSpace(Seed) ? Seed : "0";

            if (!Nodes.Any()) {
                Console.WriteLine("Missing the Required Nodes Data");
                return;
            }

            // Create reference for JS to call back to C#
            dotNetHelper = DotNetObjectReference.Create(this);

            // Import the colocated JavaScript module
            jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Shared/NetworkChart.razor.js?v=" + Guid.NewGuid().ToString().Replace("-", ""));

            await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);
            await RenderChart();
        }
    }

    // === PUBLIC METHODS ===
    public async Task RenderChart()
    {
        CalculateItems();

        var data = new {
            nodes = Nodes.ToArray(),
            edges = Relationships.ToArray(),
        };

        if (jsModule != null) {
            if (_firstRender) {
                _firstRender = false;
                await jsModule.InvokeVoidAsync("ChartRender",
                    _elementId, data, _networkSolver, _seed, StartNodeId);
            } else {
                await jsModule.InvokeVoidAsync("ChartUpdate", data, StartNodeId);
            }
        }
    }

    public async Task<string> RandomizeLayout()
    {
        if (jsModule != null) {
            _seed = await jsModule.InvokeAsync<string>("RandomizeLayout", StartNodeId);
        }
        return _seed;
    }

    public async Task UpdateNetworkSolver(string solver)
    {
        _networkSolver = solver;
        if (jsModule != null) {
            await jsModule.InvokeVoidAsync("UpdateSolver", _networkSolver);
        }
    }

    // === JS CALLBACKS (invoked from JavaScript) ===
    [JSInvokable]
    public void ElementSelected(string element)
    {
        if (OnElementSelected != null) {
            OnElementSelected.DynamicInvoke(element);
        }
    }

    [JSInvokable]
    public void RelationshipSelected(string element)
    {
        if (OnRelationshipSelected != null) {
            OnRelationshipSelected.DynamicInvoke(element);
        }
    }

    // === HELPER METHODS ===
    protected void CalculateItems()
    {
        foreach (var item in Nodes) {
            if (item.font != null) {
                if (String.IsNullOrWhiteSpace(item.font.background)) {
                    item.font.background = "transparent";
                }
                if (String.IsNullOrWhiteSpace(item.font.color)) {
                    item.font.color = "black";
                }
                if (String.IsNullOrWhiteSpace(item.font.align)) {
                    item.font.align = "bottom";
                }
            }
        }

        foreach (var item in Relationships) {
            if (item.font == null) {
                item.font = new NetworkNodeFont();
            }
            if (String.IsNullOrWhiteSpace(item.font.background)) {
                item.font.background = "transparent";
            }
            if (String.IsNullOrWhiteSpace(item.font.color)) {
                item.font.color = "black";
            }
        }
    }

    // === DATA MODELS ===
    public class NetworkNode
    {
        public string id { get; set; } = "";
        public string label { get; set; } = "";
        public string title { get; set; } = "";          // Tooltip
        public string shape { get; set; } = "icon";
        public string color { get; set; } = "#fff";
        public NetworkNodeIcon icon { get; set; } = new NetworkNodeIcon();
        public NetworkNodeFont? font { get; set; }
    }

    public class NetworkNodeFont
    {
        public string align { get; set; } = "bottom";
        public string? background { get; set; }
        public string? color { get; set; }
    }

    public class NetworkNodeIcon
    {
        public string face { get; set; } = "FontAwesome";
        public string code { get; set; } = "f059";       // Unicode for icon
        public int size { get; set; } = 50;
        public string? color { get; set; }
    }

    public class NetworkRelationship
    {
        public string id { get; set; } = "";
        public string from { get; set; } = "";           // Source node ID
        public string to { get; set; } = "";             // Target node ID
        public string label { get; set; } = "";
        public string title { get; set; } = "";          // Tooltip
        public string? color { get; set; }
        public string arrows { get; set; } = "to";       // Arrow direction
        public bool dashes { get; set; }                 // Dashed line
        public NetworkNodeFont? font { get; set; }
    }
}
```

---

## Part 3: The JavaScript Module

### NetworkChart.razor.js

```javascript
var chartData = null;
var container = null;
var dotNetHelper;
var network = null;

var networkOptions = {
    nodes: { font: { strokeWidth: 0 } },
    edges: { font: { strokeWidth: 0 } },
    interaction: {
        dragNodes: false,
        dragView: true,
        hideEdgesOnDrag: false,
        hideEdgesOnZoom: false,
        hover: true,
        navigationButtons: true,
        tooltipDelay: 0,
    },
    groups: {
        useDefaultGroups: false,
    },
    layout: {
        randomSeed: 0,
    },
    physics: {
        enabled: true,
        barnesHut: {
            theta: 0.5,
            gravitationalConstant: -2000,
            centralGravity: 0.5,
            springLength: 95,
            springConstant: 0.04,
            damping: 0.09,
            avoidOverlap: 100
        },
        repulsion: {
            centralGravity: 0.2,
            springLength: 200,
            springConstant: 0.05,
            nodeDistance: 100,
            damping: 0.09
        },
        maxVelocity: 50,
        minVelocity: 0.1,
        solver: 'repulsion',
        stabilization: {
            enabled: true,
            iterations: 1000,
            updateInterval: 100,
            onlyDynamicEdges: false,
            fit: true
        },
        timestep: 0.5,
        adaptiveTimestep: true,
        wind: { x: 0, y: 0 }
    }
};

// Initialize the chart
export function ChartRender(elementId, data, solver, seed, startNodeId) {
    chartData = FixChartIcons(data);

    networkOptions.physics.solver = solver;
    networkOptions.layout.randomSeed = seed;

    // Scale node distance based on count
    var nodeDistance = chartData.nodes.length * 15;
    if (nodeDistance < 50) nodeDistance = 50;
    else if (nodeDistance > 500) nodeDistance = 500;
    networkOptions.physics.repulsion.nodeDistance = nodeDistance;

    container = document.getElementById(elementId);
    network = new vis.Network(container, chartData, networkOptions);

    SetFocusToStartNode(startNodeId);

    // Handle selection events - call back to C#
    network.on("select", function (e) {
        var node = "";
        if (e.nodes && e.nodes.length > 0) {
            node = e.nodes[0];
        }

        if (node != "") {
            dotNetHelper.invokeMethod("ElementSelected", node);
            return;
        }

        var edge = "";
        if (e.edges && e.edges.length > 0) {
            edge = e.edges[0];
        }
        if (edge != "") {
            dotNetHelper.invokeMethod("RelationshipSelected", edge);
        }
    });
}

// Update chart with new data
export function ChartUpdate(data, startNodeId) {
    chartData = FixChartIcons(data);
    network.setData(chartData);
    SetFocusToStartNode(startNodeId);
}

// Randomize the layout
export function RandomizeLayout(startNodeId) {
    networkOptions.layout.randomSeed = undefined;
    network = new vis.Network(container, chartData, networkOptions);
    SetFocusToStartNode(startNodeId);
    var seed = network.getSeed();
    return seed;
}

// Change physics solver
export function UpdateSolver(solver) {
    networkOptions.physics.solver = solver;
    network.setOptions(networkOptions);
    network.redraw();
}

// Store reference to C# component
export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

// Helper: Focus on a specific node
function SetFocusToStartNode(startNodeId) {
    if (startNodeId) {
        network.unselectAll();
        network.setSelection({ nodes: [startNodeId] });
    }
}

// Helper: Convert icon codes for vis.js
function FixChartIcons(data) {
    data.nodes.forEach(function (item) {
        if (item.icon?.code) {
            item.icon.code = String.fromCharCode("0x" + item.icon.code);
        }
        item.title = htmlTitle(item.title);
    });
    return data;
}

// Helper: Convert HTML string to DOM element for tooltip
function htmlTitle(html) {
    const container = document.createElement("div");
    container.innerHTML = html;
    return container;
}
```

---

## Part 4: Using the Component

### Basic Usage

```razor
@if (_nodes.Any()) {
    <NetworkChart
        Nodes="_nodes"
        Relationships="_relationships"
        OnElementSelected="OnNodeClicked"
        OnRelationshipSelected="OnEdgeClicked" />
}

@code {
    protected List<NetworkChart.NetworkNode> _nodes = new();
    protected List<NetworkChart.NetworkRelationship> _relationships = new();

    protected override void OnInitialized()
    {
        // Add nodes
        _nodes.Add(new NetworkChart.NetworkNode {
            id = "server-1",
            label = "Web Server",
            title = "Production web server",
            icon = new NetworkChart.NetworkNodeIcon {
                code = "f233",  // FontAwesome server icon
                color = "#28a745",
                size = 50
            }
        });

        _nodes.Add(new NetworkChart.NetworkNode {
            id = "db-1",
            label = "Database",
            title = "PostgreSQL primary",
            icon = new NetworkChart.NetworkNodeIcon {
                code = "f1c0",  // FontAwesome database icon
                color = "#007bff",
                size = 50
            }
        });

        // Add relationship
        _relationships.Add(new NetworkChart.NetworkRelationship {
            id = "rel-1",
            from = "server-1",
            to = "db-1",
            label = "connects to",
            arrows = "to",
            color = "#6c757d"
        });
    }

    protected async Task OnNodeClicked(string nodeId)
    {
        // Handle node selection
        Console.WriteLine($"Node clicked: {nodeId}");
    }

    protected async Task OnEdgeClicked(string edgeId)
    {
        // Handle edge selection
        Console.WriteLine($"Edge clicked: {edgeId}");
    }
}
```

### With Physics Solver Selection

```razor
<div class="mb-2">
    <label for="solver">Physics Solver</label>
    <select class="form-select" id="solver" @onchange="SolverChanged">
        <option value="repulsion">Repulsion</option>
        <option value="barnesHut">Barnes-Hut</option>
        <option value="forceAtlas2Based">Force Atlas 2</option>
        <option value="hierarchicalRepulsion">Hierarchical</option>
    </select>

    <button type="button" class="btn btn-secondary" @onclick="RandomizeLayout">
        Randomize Layout
    </button>
</div>

<NetworkChart @ref="_chart"
    NetworkSolver="@_solver"
    Nodes="_nodes"
    Relationships="_relationships" />

@code {
    NetworkChart? _chart;
    protected string _solver = "repulsion";

    protected async Task SolverChanged(ChangeEventArgs e)
    {
        _solver = e.Value?.ToString() ?? "repulsion";
        if (_chart != null) {
            await _chart.UpdateNetworkSolver(_solver);
        }
    }

    protected async Task RandomizeLayout()
    {
        if (_chart != null) {
            var newSeed = await _chart.RandomizeLayout();
            // Optionally save newSeed for layout persistence
        }
    }
}
```

---

## Part 5: Building Dependency Trees

For complex dependency visualization (like DependencyManager), build the tree recursively:

```csharp
protected async Task RenderDependencyChart(DataObjects.DependencyItem rootItem)
{
    var nodes = new List<NetworkChart.NetworkNode>();
    var relationships = new List<NetworkChart.NetworkRelationship>();

    // Get all related items recursively
    var tree = GetDependencyTree(rootItem, new List<Guid>());

    foreach (var item in tree.Items) {
        // Size based on depth (root is largest)
        int size = item.Depth switch {
            0 => 100,
            1 => 70,
            2 => 45,
            3 => 30,
            _ => 20
        };

        nodes.Add(new NetworkChart.NetworkNode {
            id = item.DependencyItemId.ToString(),
            label = item.ItemName.Replace(" ", "\n"),  // Wrap text
            title = BuildTooltipHtml(item),
            icon = new NetworkChart.NetworkNodeIcon {
                code = GetIconCode(item.DependencyTypeId),
                color = GetTypeColor(item.DependencyTypeId),
                size = size
            }
        });
    }

    foreach (var dep in tree.Dependencies) {
        relationships.Add(new NetworkChart.NetworkRelationship {
            id = dep.DependencyId.ToString(),
            from = dep.DependencyItemId.ToString(),
            to = dep.RelatedItemId.ToString(),
            label = GetRelationshipLabel(dep.DependencyRelationship),
            color = GetRelationshipColor(dep.DependencyRelationship),
            arrows = "to",
            dashes = dep.DependencyRelationship == DependencyRelationship.Instance
        });
    }

    _nodes = nodes;
    _relationships = relationships;
    StateHasChanged();

    if (_chart != null) {
        await _chart.RenderChart();
    }
}
```

---

## Part 6: Key Patterns

### DotNetObjectReference Pattern

This allows JavaScript to call C# methods:

```csharp
// In C# - create reference
dotNetHelper = DotNetObjectReference.Create(this);
await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);

// In C# - mark method as callable from JS
[JSInvokable]
public void ElementSelected(string element) { ... }
```

```javascript
// In JavaScript - store reference
export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

// In JavaScript - call C# method
dotNetHelper.invokeMethod("ElementSelected", nodeId);
```

### Colocated .razor.js Pattern

```csharp
// Import JS file next to the component
jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
    "import",
    "./Shared/NetworkChart.razor.js?v=" + Guid.NewGuid().ToString().Replace("-", ""));
```

The `?v=` query string with GUID ensures cache busting during development.

### Layout Persistence

Save and restore the layout seed:

```csharp
// Save the current layout seed
var seed = await _chart.RandomizeLayout();
item.LayoutSeed = seed;
await SaveItem(item);

// Restore layout on load
<NetworkChart Seed="@item.LayoutSeed" ... />
```

---

## Part 7: Required Dependencies

### NuGet Packages

None required - vis.js is loaded via CDN or local JS.

### JavaScript Libraries

Add vis.js to your `index.html` or `_Host.cshtml`:

```html
<!-- vis.js Network -->
<script src="https://unpkg.com/vis-network/standalone/umd/vis-network.min.js"></script>

<!-- FontAwesome for icons -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
```

### CSS

Add basic styles for the chart container:

```css
/* Tooltip styling */
.tooltip-title {
    font-weight: bold;
    font-size: 1.1em;
}

/* Chart container */
.network-chart-container {
    width: 100%;
    height: 600px;
    border: 1px solid #ddd;
    background: #fff;
}
```

---

## Quick Reference

### Physics Solvers

| Solver | Best For |
|--------|----------|
| `repulsion` | General purpose, good default |
| `barnesHut` | Large graphs (100+ nodes) |
| `forceAtlas2Based` | Clustered data |
| `hierarchicalRepulsion` | Tree structures |

### Node Shapes

- `icon` - FontAwesome icon (default)
- `dot` - Simple circle
- `square` - Square
- `diamond` - Diamond
- `triangle` - Triangle
- `box` - Rectangle with label inside

### Arrow Options

- `to` - Arrow at target
- `from` - Arrow at source
- `to, from` - Bidirectional

---

*Category: 008_components*
*Last Updated: 2025-12-23*
*Source: Private repo "DependencyManager"*
